using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public enum MapGenV2StatusKind
    {
        Valid,
        Warning,
        Error,
        Stale,
        Accepted,
        Generated,
        ManualOverride,
        Locked,
        MissingReference,
        RuntimeSafe,
        Pending
    }

    public readonly struct MapGenV2StatusStyle
    {
        public readonly string Label;
        public readonly string IconName;
        public readonly Color Color;

        public MapGenV2StatusStyle(string label, string iconName, Color color)
        {
            Label = label;
            IconName = iconName;
            Color = color;
        }
    }

    public static class MapGenV2StatusPresentation
    {
        public static MapGenV2StatusStyle For(MapGenV2StatusKind kind)
        {
            switch (kind)
            {
                case MapGenV2StatusKind.Valid:
                    return new MapGenV2StatusStyle("Valid", "TestPassed", new Color(0.24f, 0.68f, 0.38f, 1f));
                case MapGenV2StatusKind.Warning:
                    return new MapGenV2StatusStyle("Warning", "console.warnicon", new Color(0.95f, 0.67f, 0.18f, 1f));
                case MapGenV2StatusKind.Error:
                    return new MapGenV2StatusStyle("Error", "console.erroricon", new Color(0.82f, 0.22f, 0.18f, 1f));
                case MapGenV2StatusKind.Stale:
                    return new MapGenV2StatusStyle("Stale", "d_Refresh", new Color(0.95f, 0.73f, 0.24f, 1f));
                case MapGenV2StatusKind.Accepted:
                    return new MapGenV2StatusStyle("Accepted", "TestPassed", new Color(0.38f, 0.72f, 0.42f, 1f));
                case MapGenV2StatusKind.Generated:
                    return new MapGenV2StatusStyle("Generated", "d_PreMatCube", new Color(0.28f, 0.52f, 0.9f, 1f));
                case MapGenV2StatusKind.ManualOverride:
                    return new MapGenV2StatusStyle("Manual Override", "d_editicon.sml", new Color(0.1f, 0.75f, 0.95f, 1f));
                case MapGenV2StatusKind.Locked:
                    return new MapGenV2StatusStyle("Locked", "IN LockButton on", new Color(0.86f, 0.56f, 0.18f, 1f));
                case MapGenV2StatusKind.MissingReference:
                    return new MapGenV2StatusStyle("Missing Reference", "console.erroricon", new Color(0.8f, 0.18f, 0.16f, 1f));
                case MapGenV2StatusKind.RuntimeSafe:
                    return new MapGenV2StatusStyle("Runtime Safe", "d_UnityEditor.InspectorWindow", new Color(0.2f, 0.66f, 0.62f, 1f));
                default:
                    return new MapGenV2StatusStyle("Pending", "d_Toggle Icon", new Color(0.42f, 0.42f, 0.42f, 1f));
            }
        }
    }
}
