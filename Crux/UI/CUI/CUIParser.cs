using AngleSharp;
using AngleSharp.Dom;

namespace Crux.UI.CUI;

public class CUIParser
{
    private readonly string input;

    public static readonly Dictionary<string, Func<UINode>> TagMap = new()
    {
        { "p", () => new UIText() },
        { "div", () => new UIContainer() }
    };

    public CUIParser(string input)
    {
        this.input = input;
    }

    public UINode? Parse()
    {
        IConfiguration config = Configuration.Default;
        IBrowsingContext context = BrowsingContext.New(config);
        IDocument document = context.OpenAsync(req => req.Content(input)).Result;
        IElement body = document.Body ?? document.DocumentElement;

        return ConvertFromAngleSharp(body.FirstChild!);
    }

    UINode ConvertFromAngleSharp(INode angleSharpNode)
    {
        if (angleSharpNode is IElement angleSharpElement)
        {
            string tagName = angleSharpElement.TagName.ToLower();

            if (!TagMap.TryGetValue(tagName, out var constructor))
                return null!;

            UINode node = constructor(); 

            if (node is UIContainer)
            {
                UIContainer casted = (node as UIContainer)!;

                foreach (INode child in angleSharpElement.ChildNodes)
                {
                    UINode createdChild = ConvertFromAngleSharp(child);
                    if (createdChild != null)
                        casted.Children.Add(createdChild);
                }
            }else if (node is UIText)
            {
                UIText casted = (node as UIText)!;

                casted.Text = string.Join("", angleSharpElement.ChildNodes
                    .OfType<IText>()
                    .Select(t => t.Text.Trim()));
            }

            return node;
        }

        return null!;
    }
}
