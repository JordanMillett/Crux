using AngleSharp;
using AngleSharp.Dom;
using Crux.Components;
using Crux.Utilities.Helpers;
using AngleSharp.Css.Dom;
using System.Text.RegularExpressions;

namespace Crux.CUI;

public class CUIParser
{
    private readonly string input;

    public static Dictionary<string, string> DefaultCSSProperties = new()
    {
        {"display",             "block"},
        {"width",               "auto"},
        {"height",              "auto"},
        {"background-color",    ""},
        {"background-image",    ""},
        {"font-size",           "16px"},
        {"color",               "rgba(255, 255, 255, 1.0)"},
        {"padding-top",         "0px"},
        {"padding-right",       "0px"},
        {"padding-bottom",      "0px"},
        {"padding-left",        "0px"},
    };

    public Dictionary<string, string> ExtractCSSProperties(ICssStyleDeclaration styleData)
    {
        Dictionary<string, string> extracted = [];

        foreach (string key in DefaultCSSProperties.Keys)
        {
            string property = styleData.GetPropertyValue(key);
            if(string.IsNullOrEmpty(property))
                extracted[key] = DefaultCSSProperties[key];
            else
                extracted[key] = property;
        }

        return extracted;
    }

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
            Dictionary<string, string> style = ExtractCSSProperties(angleSharpElement.GetStyle());
            string tagName = angleSharpElement.TagName.ToLower();
            
            //Logger.Log($"IElement parsed: {tagName}");

            switch (tagName)
            {
                case "body": 
                    if(!string.IsNullOrEmpty(style["background-color"])) //OPTIONAL
                    {
                        cruxNode = new CUIPanel(canvas);
                        (cruxNode as CUIPanel)!.Background = ColorHelper.RGBAStringToColor4(style["background-color"]);
                    }else
                    {
                        cruxNode = new CUIEmpty(canvas);
                    }

                    cruxNode.Bounds.Width = new CUIUnit(CUIUnitType.ViewportWidth, 100);
                    cruxNode.Bounds.Height = new CUIUnit(CUIUnitType.ViewportHeight, 100);
                    cruxNode.Bounds.LayoutMode = CUILayoutMode.Block;
                break;
                case "div":    
                    if(!string.IsNullOrEmpty(style["background-color"])) //OPTIONAL
                    {
                        cruxNode = new CUIPanel(canvas);
                        (cruxNode as CUIPanel)!.Background = ColorHelper.RGBAStringToColor4(style["background-color"]);
                    }

                    if(!string.IsNullOrEmpty(style["background-image"])) //OPTIONAL
                    {
                        if(cruxNode == null)
                            cruxNode = new CUIPanel(canvas);
                        
                        CUIPanel.ShaderSingleton.ColorTexturePath = style["background-image"].Substring(5, style["background-image"].Length - 5 - 2);
                        CUIPanel.ShaderSingleton.GenerateTextureID();
                    }

                    cruxNode.Bounds.LayoutMode = style["display"] switch
                    {
                        "inline-block" => CUILayoutMode.InlineBlock,
                        "block" => CUILayoutMode.Block,
                        _ => CUILayoutMode.Block
                    };
                break;
                case "p": 
                    cruxNode = new CUIText(canvas);

                    cruxNode.Bounds.LayoutMode = CUILayoutMode.InlineBlock;

                    (cruxNode as CUIText)!.FontSize = CUIUnit.Parse(style["font-size"]);
                    (cruxNode as CUIText)!.FontColor = ColorHelper.RGBAStringToColor4(style["color"]);

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

            if(cruxNode == null)
                cruxNode = new CUIEmpty(canvas);
            
            if(!string.IsNullOrEmpty(angleSharpElement.Id))
                canvas.NodesRefs.Add(angleSharpElement.Id, cruxNode);

            cruxNode.Bounds.Width = CUIUnit.Parse(style["width"]);
            cruxNode.Bounds.Height = CUIUnit.Parse(style["height"]);
            cruxNode.Bounds.Padding.Top = CUIUnit.Parse(style["padding-top"]);
            cruxNode.Bounds.Padding.Right = CUIUnit.Parse(style["padding-right"]);
            cruxNode.Bounds.Padding.Bottom = CUIUnit.Parse(style["padding-bottom"]);
            cruxNode.Bounds.Padding.Left = CUIUnit.Parse(style["padding-left"]);
        }

        if(cruxNode == null)
            cruxNode = new CUIEmpty(canvas);

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
