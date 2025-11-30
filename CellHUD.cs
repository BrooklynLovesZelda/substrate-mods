using BepInEx;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;

namespace CellHUDMod
{
    [BepInPlugin("com.cellhud.mod", "Cell Info HUD", "1.0.0")]
    public class CellHUDPlugin : BaseUnityPlugin
    {
        private CellInfoHUD hud;

        private void Awake()
        {
            Logger.LogInfo("Cell Info HUD v1.0.0");
            Logger.LogInfo("Press H to toggle HUD");
        }

        private void Start()
        {
            var obj = new GameObject("CellInfoHUD");
            hud = obj.AddComponent<CellInfoHUD>();
            DontDestroyOnLoad(obj);
        }

        private void Update()
        {
            if (hud == null) return;

            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                hud.Toggle();
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                hud.SelectAtMouse();
            }
        }
    }

    public class CellInfoHUD : MonoBehaviour
    {
        public bool visible = true;
        private GUIStyle labelStyle, boxStyle, headerStyle;
        private Texture2D bgTex;
        private CellBody selected;
        private int selectedCellIndex = 0;
        private string ecoText = "", cellText = "";
        private float cyto, cytoMax;
        private readonly StringBuilder sb = new(200);
        private float timer;

        private void Start()
        {
            bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0, 0, 0, 0.75f));
            bgTex.Apply();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer > 0.15f)
            {
                UpdateData();
                timer = 0;
            }
        }

        private void OnGUI()
        {
            if (!visible) return;
            InitStyles();

            DrawEco();
            if (selected != null)
                DrawCell();
            else
                DrawHint();
        }

        void InitStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    richText = true,
                    wordWrap = false,
                    normal = { textColor = new Color(0.95f, 0.95f, 0.95f) }
                };
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    richText = true,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.3f, 1f, 0.8f) }
                };
                boxStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = bgTex }
                };
            }
        }

        void DrawEco()
        {
            float w = 200f;
            float headerHeight = headerStyle.CalcSize(new GUIContent("ECOSYSTEM")).y + 4;
            float textHeight = labelStyle.CalcHeight(new GUIContent(ecoText), w - 12);
            float h = 12 + headerHeight + textHeight;

            float x = 10f, y = 10f;

            GUI.Box(new Rect(x, y, w, h), "", boxStyle);
            GUI.Label(new Rect(x, y + 6, w, headerHeight), "ECOSYSTEM", headerStyle);
            GUI.Label(new Rect(x + 6, y + 6 + headerHeight, w - 12, textHeight), ecoText, labelStyle);
        }

        void DrawCell()
        {
            var allCells = CellBody.AllCells;
            int total = allCells != null ? allCells.Count : 0;
            string headerText = $"CELL #{selectedCellIndex + 1} ({selectedCellIndex + 1}/{total})";

            float w = 220f;
            float headerHeight = headerStyle.CalcSize(new GUIContent(headerText)).y + 4;
            float barHeight = 20f;
            float textHeight = labelStyle.CalcHeight(new GUIContent(cellText), w - 12);
            float h = 12 + headerHeight + barHeight + 4 + textHeight;

            float x = 10f, y = Screen.height - h - 10f;

            GUI.Box(new Rect(x, y, w, h), "", boxStyle);
            GUI.Label(new Rect(x, y + 6, w, headerHeight), headerText, headerStyle);

            DrawCytosolBar(x + 6, y + 6 + headerHeight, w - 12);
            GUI.Label(new Rect(x + 6, y + 6 + headerHeight + barHeight + 4, w - 12, textHeight), cellText, labelStyle);
        }

        void DrawCytosolBar(float x, float y, float width)
        {
            float h = 16;
            float fill = width * Mathf.Clamp01((cyto + 0.5f));

            GUI.DrawTexture(new Rect(x, y, width, h), bgTex);
            GUI.color = GetCytoColor(cyto);
            GUI.DrawTexture(new Rect(x, y, fill, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Box(new Rect(x - 1, y - 1, width + 2, h + 2), "", GUI.skin.box);

            string s = $"Cytosol: {cyto:F2} / {cytoMax:F2}";
            var style = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            float textH = style.CalcSize(new GUIContent(s)).y;
            GUI.Label(new Rect(x, y + (h - textH) * 0.5f, width, textH), s, style);
        }

        void DrawHint()
        {
            string hint = "Click a cell to view info";
            float w = 220f;
            float h = labelStyle.CalcSize(new GUIContent(hint)).y + 20f;
            float x = 10f, y = Screen.height - h - 10f;

            GUI.Box(new Rect(x, y, w, h), "", boxStyle);
            var style = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                fontSize = 12,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            GUI.Label(new Rect(x, y + (h - style.CalcSize(new GUIContent(hint)).y) * 0.5f, w, style.CalcSize(new GUIContent(hint)).y), hint, style);
        }

        void UpdateData()
        {
            var cells = CellBody.AllCells;
            int cellCount = cells != null ? cells.Count : 0;
            int food = EnergyParticle.AliveCount;

            sb.Clear();
            sb.AppendLine($"Cells: <color=#00ff88>{cellCount}</color>");
            sb.AppendLine($"Food: <color=#ffdd44>{food}</color>");
            ecoText = sb.ToString();

            if (cells == null || cells.Count == 0)
            {
                selected = null;
                return;
            }

            bool found = false;
            if (selected != null && selected.gameObject != null && selected.gameObject.activeInHierarchy)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    if (cells[i] == selected)
                    {
                        selectedCellIndex = i;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                selected = cells[0];
                selectedCellIndex = 0;
            }

            cyto = selected.GetCytosolBuffer();
            cytoMax = selected.GetEffectiveCytosolMaxBuffer();

            float radius = selected.GetRadius();
            float speed = selected.GetVelocity().magnitude;

            string status;
            if (cyto < -0.3f)
                status = "<color=#ff4444>Starving</color>";
            else if (cyto > 0.3f)
                status = "<color=#44ff88>Thriving</color>";
            else if (cyto > 0f)
                status = "<color=#88ff88>Healthy</color>";
            else
                status = "<color=#ffaa44>Hungry</color>";

            sb.Clear();
            sb.AppendLine($"Radius: <color=#aaddff>{radius:F2}</color>");
            sb.AppendLine($"Speed: <color=#ffaa44>{speed:F2}</color> u/s");
            sb.Append($"Status: {status}");
            cellText = sb.ToString();
        }

        Color GetCytoColor(float v)
        {
            if (v < -0.3f) return new Color(1f, 0.3f, 0.3f);
            if (v < 0f) return new Color(1f, 0.8f, 0.2f);
            return new Color(0.3f, 1f, 0.8f);
        }

        public void Toggle() => visible = !visible;

        public void SelectAtMouse()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var mp = Mouse.current.position.ReadValue();
            var wp = cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, -cam.transform.position.z));
            wp.z = 0;

            var org = FindOrganelle(wp, 3f);
            if (org != null)
            {
                var cb = org.GetComponentInParent<CellBody>();
                if (cb != null)
                {
                    selected = cb;
                    return;
                }
            }

            CellBody best = null;
            float bestDist = float.MaxValue;
            foreach (var c in CellBody.AllCells)
            {
                if (c == null) continue;
                float d = Vector2.Distance(wp, c.transform.position);
                if (d <= c.GetRadius() && d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }

            if (best != null) selected = best;
        }

        Organelle FindOrganelle(Vector3 pos, float r)
        {
            var all = Object.FindObjectsByType<Organelle>(FindObjectsSortMode.None);
            Organelle closest = null;
            float minDist = float.MaxValue;
            foreach (var o in all)
            {
                if (o == null) continue;
                float d = Vector2.Distance(pos, o.transform.position);
                if (d < r && d < minDist)
                {
                    minDist = d;
                    closest = o;
                }
            }
            return closest;
        }

        private void OnDestroy()
        {
            if (bgTex != null) Destroy(bgTex);
        }
    }
}
