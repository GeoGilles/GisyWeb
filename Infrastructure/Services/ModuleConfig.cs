namespace GisyWeb.Services
{
    public class ModuleConfig
    {
        public string Code { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string ThemeColor { get; set; } = "#ffc107";
        public bool IsActive { get; set; } = true;
    }

    public class SessionContext
    {
        public string CurrentModule { get; set; } = "SIAS";
    }
}