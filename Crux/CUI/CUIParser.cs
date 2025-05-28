using AngleSharp;
using AngleSharp.Dom;
using Crux.Components;
using Crux.Utilities.Helpers;
using AngleSharp.Css.Dom;
using System.Text.RegularExpressions;

namespace Crux.CUI;

/*
Supported Features

div
- background-color

p
- color
- font-size
*/

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
                        (cruxNode as CUIText)!.FontSize = CUIUnit.Parse(textSize);

                    string textColor = styleData.GetPropertyValue("color");
                    if(!string.IsNullOrEmpty(textColor))
                        (cruxNode as CUIText)!.FontColor = ColorHelper.RGBAStringToColor4(textColor);

                    (cruxNode as CUIText)!.TextData = string.Concat(
                        angleSharpElement.ChildNodes.Select(node =>
                        {
                            if (node is IText textNode)
                            {
                                return CollapseText(textNode.Text);
                            }
                            else if (node is IElement child && child.TagName == "BR")
                            {
                                return "\n";
                            }
                            return string.Empty;
                        })
                    );

                break;
            }

            if(cruxNode != null)
            {
                string width = styleData.GetPropertyValue("width");
                if(!string.IsNullOrEmpty(width))
                    cruxNode.Bounds.Width = CUIUnit.Parse(width);
                else
                    cruxNode.Bounds.Width = new CUIUnit(CUIUnitType.Auto);

                string height = styleData.GetPropertyValue("height");
                if(!string.IsNullOrEmpty(height))
                    cruxNode.Bounds.Height = CUIUnit.Parse(height);
                else
                    cruxNode.Bounds.Height = new CUIUnit(CUIUnitType.Auto);
            }
        }

        if(cruxNode == null)
        {
            cruxNode = new CUIEmpty(canvas);
            cruxNode.Bounds.Width = new CUIUnit(CUIUnitType.Auto);
            cruxNode.Bounds.Height = new CUIUnit(CUIUnitType.Auto);
        }

        foreach (INode child in angleSharpNode.ChildNodes)
        {
            //Logger.Log("ENTERING...");
            CUINode createdChild = ConvertFromAngleSharp(child, canvas);
            cruxNode.Children.Add(createdChild);
            createdChild.Parent = cruxNode;
            //Logger.Log("EXITING...");
        }

        return cruxNode;
    }

    public static string CollapseText(string input)
    {
        return Regex.Replace(input, @"\s+", " ").Trim();
    }
}
