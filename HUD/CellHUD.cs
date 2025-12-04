using BepInEx;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;

namespace CellHUDMod
{
    [BepInPlugin("com.cellhud.mod", "Cell Info HUD", "1.1.0")]
    public class CellHUDPlugin : BaseUnityPlugin
    {
        private CellInfoHUD hud;

        private void Awake()
        {
            Logger.LogInfo("Cell Info HUD v1.1.0 loaded - Press H to toggle");
        }

        private void Start()
        {
            var go = new GameObject("CellInfoHUD");
            hud = go.AddComponent<CellInfoHUD>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            if (hud == null) return;
            if (Keyboard.current.hKey.wasPressedThisFrame) hud.Toggle();
            if (Mouse.current.leftButton.wasReleasedThisFrame) hud.SelectAtMouse();
        }
    }

    public class CellInfoHUD : MonoBehaviour
    {
        private bool visible = true;
        private CellBody selected;
        private int selectedIndex;

        private GUIStyle label, header, box;
        private Texture2D bg;
        private StringBuilder sb = new StringBuilder(256);

        private string ecoText = "";
        private string cellText = "";
        private float cytosolCurrent, cytosolMin, cytosolMax;
        private float refreshTimer;

        private void Start()
        {
            bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0, 0, 0, 0.75f));
            bg.Apply();
        }

        private void Update()
        {
            refreshTimer += Time.deltaTime;
            if (refreshTimer > 0.15f)
            {
                RefreshData();
                refreshTimer = 0;
            }
        }

        private void OnGUI()
        {
            if (!visible) return;
            EnsureStyles();
            DrawEcosystemPanel();
            DrawCellPanel();
        }

        private void EnsureStyles()
        {
            if (label != null) return;

            label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                richText = true,
                wordWrap = false,
                normal = { textColor = new Color(0.95f, 0.95f, 0.95f) }
            };

            header = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                richText = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 1f, 0.8f) }
            };

            box = new GUIStyle(GUI.skin.box) { normal = { background = bg } };
        }

        private void DrawEcosystemPanel()
        {
            float w = 200, x = 10, y = 10;
            float headerH = header.CalcSize(new GUIContent("ECOSYSTEM")).y + 4;
            float textH = label.CalcHeight(new GUIContent(ecoText), w - 12);
            float h = 12 + headerH + textH;

            GUI.Box(new Rect(x, y, w, h), "", box);
            GUI.Label(new Rect(x, y + 6, w, headerH), "ECOSYSTEM", header);
            GUI.Label(new Rect(x + 6, y + 6 + headerH, w - 12, textH), ecoText, label);
        }

        private void DrawCellPanel()
        {
            float w = 220, x = 10;

            if (selected == null)
            {
                DrawHint(x, w);
                return;
            }

            var cells = CellBody.AllCells;
            int total = cells?.Count ?? 0;
            string title = $"CELL #{selectedIndex + 1} ({selectedIndex + 1}/{total})";

            float headerH = header.CalcSize(new GUIContent(title)).y + 4;
            float barH = 20;
            float textH = label.CalcHeight(new GUIContent(cellText), w - 12);
            float h = 12 + headerH + barH + 4 + textH;
            float y = Screen.height - h - 10;

            GUI.Box(new Rect(x, y, w, h), "", box);
            GUI.Label(new Rect(x, y + 6, w, headerH), title, header);
            DrawCytosolBar(x + 6, y + 6 + headerH, w - 12, barH - 4);
            GUI.Label(new Rect(x + 6, y + 6 + headerH + barH + 4, w - 12, textH), cellText, label);
        }

        private void DrawHint(float x, float w)
        {
            string hint = "Click a cell to view info";
            float h = label.CalcSize(new GUIContent(hint)).y + 20;
            float y = Screen.height - h - 10;

            var hintStyle = new GUIStyle(label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                fontSize = 12,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            GUI.Box(new Rect(x, y, w, h), "", box);
            GUI.Label(new Rect(x, y, w, h), hint, hintStyle);
        }

        private void DrawCytosolBar(float x, float y, float w, float h)
        {
            float range = cytosolMax - cytosolMin;
            if (range < 0.001f) range = 1f;
            float fill = w * Mathf.Clamp01((cytosolCurrent - cytosolMin) / range);

            GUI.DrawTexture(new Rect(x, y, w, h), bg);
            GUI.color = GetCytosolColor(cytosolCurrent);
            GUI.DrawTexture(new Rect(x, y, fill, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Box(new Rect(x - 1, y - 1, w + 2, h + 2), "", GUI.skin.box);

            var barLabel = new GUIStyle(label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };
            GUI.Label(new Rect(x, y, w, h), $"Cytosol: {cytosolCurrent:F2} / {cytosolMax:F2}", barLabel);
        }

        private void RefreshData()
        {
            var cells = CellBody.AllCells;
            int cellCount = cells?.Count ?? 0;
            int foodCount = EnergyParticle.AliveCount;

            sb.Clear();
            sb.AppendLine($"Cells: <color=#00ff88>{cellCount}</color>");
            sb.Append($"Food: <color=#ffdd44>{foodCount}</color>");
            ecoText = sb.ToString();

            if (cellCount == 0)
            {
                selected = null;
                return;
            }

            if (selected != null && selected.gameObject != null && selected.gameObject.activeInHierarchy)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    if (cells[i] == selected)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                selected = cells[0];
                selectedIndex = 0;
            }

            cytosolCurrent = selected.GetCytosolBuffer();
            cytosolMin = selected.GetCytosolMinBuffer();
            cytosolMax = selected.GetEffectiveCytosolMaxBuffer();

            float radius = selected.GetRadius();
            float speed = selected.GetVelocity().magnitude;
            string status = GetStatusText(cytosolCurrent);

            sb.Clear();
            sb.AppendLine($"Radius: <color=#aaddff>{radius:F2}</color>");
            sb.AppendLine($"Speed: <color=#ffaa44>{speed:F2}</color> u/s");
            sb.Append($"Status: {status}");
            cellText = sb.ToString();
        }

        private string GetStatusText(float cyto)
        {
            if (cyto < -0.3f) return "<color=#ff4444>Starving</color>";
            if (cyto > 0.3f) return "<color=#44ff88>Thriving</color>";
            if (cyto > 0f) return "<color=#88ff88>Healthy</color>";
            return "<color=#ffaa44>Hungry</color>";
        }

        private Color GetCytosolColor(float v)
        {
            float range = cytosolMax - cytosolMin;
            float normalized = range > 0.001f ? (v - cytosolMin) / range : 0.5f;
            
            if (normalized < 0.2f) return new Color(1f, 0.3f, 0.3f);
            if (normalized < 0.4f) return new Color(1f, 0.8f, 0.2f);
            if (normalized > 0.8f) return new Color(0.2f, 0.9f, 1f);
            return new Color(0.3f, 1f, 0.8f);
        }

        public void Toggle() => visible = !visible;

        public void SelectAtMouse()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var mousePos = Mouse.current.position.ReadValue();
            var worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z));
            worldPos.z = 0;

            var cells = CellBody.AllCells;
            if (cells == null) return;

            CellBody closest = null;
            float closestDist = float.MaxValue;

            foreach (var cell in cells)
            {
                if (cell == null) continue;
                float dist = Vector2.Distance(worldPos, cell.transform.position);
                if (dist <= cell.GetRadius() * 1.2f && dist < closestDist)
                {
                    closestDist = dist;
                    closest = cell;
                }
            }

            if (closest != null) selected = closest;
        }

        private void OnDestroy()
        {
            if (bg != null) Destroy(bg);
        }
    }
}
