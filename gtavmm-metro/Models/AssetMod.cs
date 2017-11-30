namespace gtavmm_metro.Models
{
    public class AssetMod
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public string TargetRPF { get; set; }
        public bool IsUsableAssetMod { get; set; }
        public int OrderIndex { get; set; }
    }
}
