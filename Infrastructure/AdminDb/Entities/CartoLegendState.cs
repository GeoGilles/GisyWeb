using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_LEGEND_STATE")]
    public class CartoLegendState
    {
        [Key]
        [Column("STATE_ID")]
        public string StateId { get; set; } = Guid.NewGuid().ToString();

        [Column("CLIENT_CODE")] public string ClientCode { get; set; } = string.Empty;
        [Column("USER_CODE")] public string UserCode { get; set; } = string.Empty;
        [Column("UPDATED_AT")] public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        [Column("STYLE_ID")]
        public int? StyleId { get; set; }

        [ForeignKey("StyleId")]
        //public virtual Carto5LayerStyle? LinkedStyle { get; set; }

        // CONTEXTE
        [Column("SCOPE_TYPE")] public string ScopeType { get; set; } = "GLOBAL";
        [Column("CONTEXT_ID")] public int ContextId { get; set; } = 0;
        [Column("PROJECT_ID")] public int ProjectId { get; set; } = 0;
        [Column("SITE_ID")] public int SiteId { get; set; } = 0;
        [Column("LEGEND_MODE")] public string LegendMode { get; set; } = "USER";

        // OBJET
        [Column("LAYER_CODE")] public string LayerCode { get; set; } = string.Empty;
        [Column("LAYER_SOURCE")] public string LayerSource { get; set; } = "PROD";
        [Column("GEOMETRY_KIND")] public string GeometryKind { get; set; } = "UNKNOWN";

        // ÉTAT VISUEL
        [Column("SORT_INDEX")] public int SortIndex { get; set; }
        [Column("Z_INDEX")] public int ZIndex { get; set; }
        [Column("IS_VISIBLE")] public int IsVisibleInt { get; set; } = 1;
        [Column("OPACITY")] public double Opacity { get; set; } = 1.0;
        [Column("IS_SELECTABLE")] public int IsSelectableInt { get; set; } = 1;
        [Column("STYLE_JSON")] public string? StyleJson { get; set; }

        // GROUPES
        [Column("GROUP_CODE")] public string? GroupCode { get; set; }
        [Column("GROUP_ORDER")] public int GroupOrder { get; set; }
        [Column("IS_GROUP_OPEN")] public int IsGroupOpenInt { get; set; } = 1;

        // THÈMES & PRESETS
        [Column("THEME_CODE")] public string? ThemeCode { get; set; }
        [Column("IS_THEME_ACTIVE")] public int IsThemeActiveInt { get; set; } = 0;
        [Column("PRESET_CODE")] public string? PresetCode { get; set; }
        [Column("IS_PRESET_DEFAULT")] public int IsPresetDefaultInt { get; set; } = 0;

        // UI STATE
        [Column("PANEL_STATE")] public string? PanelState { get; set; }
        [Column("UI_STATE_JSON")] public string? UiStateJson { get; set; }

        // --- HELPERS (Non mappés en base, pour ton code C#) ---
        [NotMapped] public bool IsVisible { get => IsVisibleInt == 1; set => IsVisibleInt = value ? 1 : 0; }
        [NotMapped] public bool IsGroupOpen { get => IsGroupOpenInt == 1; set => IsGroupOpenInt = value ? 1 : 0; }
        [NotMapped] public bool IsThemeActive { get => IsThemeActiveInt == 1; set => IsThemeActiveInt = value ? 1 : 0; }
    }
}