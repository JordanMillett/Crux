using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Css;
using Crux.Components;
using Crux.Utilities.Helpers;
using AngleSharp.Css.Dom;
using System.Drawing;

namespace Crux.CUI;

public class CUIParser
{
    private readonly string input;

    public CUIParser(string input)
    {
        this.input = input;
    }

    public CUINode? Parse(CanvasComponent canvas)
    {
        IConfiguration config = Configuration.Default.WithCss();
        IBrowsingContext context = BrowsingContext.New(config);
        IDocument document = context.OpenAsync(req => req.Content(input)).Result;
        IElement body = document.Body ?? document.DocumentElement;

        return ConvertFromAngleSharp(body!, canvas);
    }

    CUINode ConvertFromAngleSharp(INode angleSharpNode, CanvasComponent canvas)
    {
        //Logger.Log($"INode Type: {angleSharpNode.GetType().Name}");

        CUINode cruxNode = null!;

        if (angleSharpNode is IElement angleSharpElement)
        {
            string tagName = angleSharpElement.TagName.ToLower();
            ICssStyleDeclaration styleData = angleSharpElement.GetStyle();
            //Logger.Log($"IElement parsed: {tagName}");

            switch (tagName)
            {
                case "div": 
                    string backgroundColor = styleData.GetPropertyValue("background-color");
            
                    if(!string.IsNullOrEmpty(backgroundColor))
                    {
                        cruxNode = new CUIPanel(canvas);
                        
                        (cruxNode as CUIPanel)!.Background = ColorHelper.RGBAStringToColor4(backgroundColor);
                    }
                break;
                case "p": 
                    cruxNode = new CUIText(canvas);

                    string textSize = styleData.GetPropertyValue("font-size");
                    if(!string.IsNullOrEmpty(textSize))
                    {
                        if(textSize.EndsWith("px"))
                            textSize = textSize[..^2];

                        (cruxNode as CUIText)!.VirtualFontSize = float.Parse(textSize);
                    }

                    string textColor = styleData.GetPropertyValue("color");
                    if(!string.IsNullOrEmpty(textColor))
                        (cruxNode as CUIText)!.FontColor = ColorHelper.RGBAStringToColor4(textColor);

                    (cruxNode as CUIText)!.Text = string.Join("", angleSharpElement.ChildNodes
                        .OfType<IText>()
                        .Select(t => t.Text.Trim()));
                break;
            }
        }

        if(cruxNode == null)
            cruxNode = new CUIEmpty(canvas);

        foreach (INode child in angleSharpNode.ChildNodes)
        {
            //Logger.Log("ENTERING...");
            CUINode createdChild = ConvertFromAngleSharp(child, canvas);
            cruxNode.Children.Add(createdChild);
            //Logger.Log("EXITING...");
        }

        return cruxNode;
    }
}
