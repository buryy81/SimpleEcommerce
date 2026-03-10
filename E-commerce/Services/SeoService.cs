using SimpleEcommerce.Models;

namespace SimpleEcommerce.Services;

public class SeoService
{
    public SeoMeta Meta { get; private set; } = new SeoMeta();

    public void SetMeta(string title, string description, string image, string url, string type = "website")
    {
        Meta.Title = title;
        Meta.Description = description;
        Meta.Image = image;
        Meta.Url = url;
        Meta.Type = type;
    }
}
