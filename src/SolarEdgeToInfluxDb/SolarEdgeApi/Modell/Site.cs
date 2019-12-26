namespace SolarEdgeToInfluxDb.SolarEdgeApi.Modell
{
    public class Site
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SiteLocation Location { get; set; }

    }
}
