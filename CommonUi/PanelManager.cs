

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Eto.Forms;

    namespace CommonUi
    {
        public class LaunchedPanelInfo
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Name { get; set; } = string.Empty;
            public string TypeName { get; set; } = string.Empty;
            public DateTime LaunchedAt { get; set; } = DateTime.Now;
            public Control PanelReference { get; set; }
        }

        public static class PanelManager
        {
            private static readonly Dictionary<string, LaunchedPanelInfo> _launchedPanels = new Dictionary<string, LaunchedPanelInfo>();

            public static void RegisterPanel(string name, Control panel)
            {
                var info = new LaunchedPanelInfo
                {
                    Name = name,
                    TypeName = panel.GetType().Name,
                    PanelReference = panel
                };

                _launchedPanels[info.Id] = info;
            }

            public static void UnregisterPanel(string id)
            {
                if (_launchedPanels.ContainsKey(id))
                {
                    _launchedPanels.Remove(id);
                }
            }

            public static LaunchedPanelInfo GetPanel(string id)
            {
                return _launchedPanels.TryGetValue(id, out var panel) ? panel : null;
            }

            public static List<LaunchedPanelInfo> GetAllPanels()
            {
                return _launchedPanels.Values.ToList();
            }
        }
    }
