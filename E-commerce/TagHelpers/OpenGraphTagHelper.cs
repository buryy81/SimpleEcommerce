using Microsoft.AspNetCore.Razor.TagHelpers;
using SimpleEcommerce.Services;

namespace SimpleEcommerce.TagHelpers;

[HtmlTargetElement("open-graph")]
public class OpenGraphTagHelper : TagHelper
{
    private readonly SeoService _seo;

    public OpenGraphTagHelper(SeoService seo)
    {
        _seo = seo;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var meta = _seo.Meta;

        output.TagName = null;

        output.Content.SetHtmlContent($@"
<meta property='og:title' content='{meta.Title}' />
<meta property='og:description' content='{meta.Description}' />
<meta property='og:image' content='{meta.Image}' />
<meta property='og:url' content='{meta.Url}' />
<meta property='og:type' content='{meta.Type}' />

<meta name='twitter:card' content='summary_large_image' />
<meta name='twitter:title' content='{meta.Title}' />
<meta name='twitter:description' content='{meta.Description}' />
<meta name='twitter:image' content='{meta.Image}' />
");
    }
}
