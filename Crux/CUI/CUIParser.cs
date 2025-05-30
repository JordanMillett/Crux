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
                case "body": 
                    string bodyColor = styleData.GetPropertyValue("background-color");
            
                    if(!string.IsNullOrEmpty(bodyColor))
                    {
                        cruxNode = new CUIPanel(canvas);
                        (cruxNode as CUIPanel)!.Background = ColorHelper.RGBAStringToColor4(bodyColor);
                    }else
                    {
                        cruxNode = new CUIEmpty(canvas);
                    }

                    cruxNode.Bounds.Width = new CUIUnit(CUIUnitType.ViewportWidth, 100);
                    cruxNode.Bounds.Height = new CUIUnit(CUIUnitType.ViewportHeight, 100);
                break;
                case "div": 
                    string backgroundColor = styleData.GetPropertyValue("background-color");
            
                    if(!string.IsNullOrEmpty(backgroundColor))
                    {
                        cruxNode = new CUIPanel(canvas);
                        (cruxNode as CUIPanel)!.Background = ColorHelper.RGBAStringToColor4(backgroundColor);
                    }

                    string backgroundImage = styleData.GetPropertyValue("background-image");
            
                    if(!string.IsNullOrEmpty(backgroundImage))
                    {
                        if(cruxNode == null)
                            cruxNode = new CUIPanel(canvas);
                        
                        CUIPanel.ShaderSingleton.ColorTexturePath = backgroundImage.Substring(5, backgroundImage.Length - 5 - 2);
                        CUIPanel.ShaderSingleton.GenerateTextureID();
                        
                    }

                    string layout = styleData.GetPropertyValue("display");
                    if(!string.IsNullOrEmpty(layout))
                    {
                        cruxNode.Bounds.LayoutMode = layout switch
                        {
                            "inline-block" => CUILayoutMode.InlineBlock,
                            "block" => CUILayoutMode.Block,
                            _ => CUILayoutMode.Block
                        };
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

            if(cruxNode == null)
                cruxNode = new CUIEmpty(canvas);
            
            if(!string.IsNullOrEmpty(angleSharpElement.Id))
                canvas.NodesRefs.Add(angleSharpElement.Id, cruxNode);

            string width = styleData.GetPropertyValue("width");
            if(!string.IsNullOrEmpty(width))
                cruxNode.Bounds.Width = CUIUnit.Parse(width);

            string height = styleData.GetPropertyValue("height");
            if(!string.IsNullOrEmpty(height))
                cruxNode.Bounds.Height = CUIUnit.Parse(height);

            string padding_top = styleData.GetPropertyValue("padding-top");
            if(!string.IsNullOrEmpty(padding_top))
                cruxNode.Bounds.Padding.Top = CUIUnit.Parse(padding_top);

            string padding_right = styleData.GetPropertyValue("padding-right");
            if(!string.IsNullOrEmpty(padding_right))
                cruxNode.Bounds.Padding.Right = CUIUnit.Parse(padding_right);

            string padding_bottom = styleData.GetPropertyValue("padding-bottom");
            if(!string.IsNullOrEmpty(padding_bottom))
                cruxNode.Bounds.Padding.Bottom = CUIUnit.Parse(padding_bottom);

            string padding_left = styleData.GetPropertyValue("padding-left");
            if(!string.IsNullOrEmpty(padding_left))
                cruxNode.Bounds.Padding.Left = CUIUnit.Parse(padding_left);
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
