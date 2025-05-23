using AngleSharp;
using AngleSharp.Dom;

namespace Crux.CUI;

public class CUIParser
{
    private readonly string input;

    public static readonly Dictionary<string, Func<CUINode>> TagMap = new()
    {
        { "p", () => new CUIText() },
        { "div", () => new CUIContainer() }
    };

    public CUIParser(string input)
    {
        this.input = input;
    }

    public CUINode? Parse()
    {
        IConfiguration config = Configuration.Default;
        IBrowsingContext context = BrowsingContext.New(config);
        IDocument document = context.OpenAsync(req => req.Content(input)).Result;
        IElement body = document.Body ?? document.DocumentElement;

        return ConvertFromAngleSharp(body.FirstChild!);
    }

    CUINode ConvertFromAngleSharp(INode angleSharpNode)
    {
        if (angleSharpNode is IElement angleSharpElement)
        {
            string tagName = angleSharpElement.TagName.ToLower();

            if (!TagMap.TryGetValue(tagName, out var constructor))
                return null!;

            CUINode cruxNode = constructor(); 

            if (cruxNode is CUIContainer)
            {
                CUIContainer casted = (cruxNode as CUIContainer)!;

                foreach (INode child in angleSharpElement.ChildNodes)
                {
                    CUINode createdChild = ConvertFromAngleSharp(child);
                    if (createdChild != null)
                        casted.Children.Add(createdChild);
                }
            }else if (cruxNode is CUIText)
            {
                CUIText casted = (cruxNode as CUIText)!;

                casted.Text = string.Join("", angleSharpElement.ChildNodes
                    .OfType<IText>()
                    .Select(t => t.Text.Trim()));
            }

            return cruxNode;
        }

        return null!;
    }
}
