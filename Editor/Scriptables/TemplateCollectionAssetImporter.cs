using System;
using System.IO;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    [ScriptedImporter(1, MSSConstants.TEMPLATE_COLLECTION_EXTENSION, -1000)]
    public class TemplateColletionAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var subAsset = ScriptableObject.CreateInstance<TemplateCollectionAsset>();
            
            //string text = File.ReadAllText(ctx.assetPath);

            
            using (var sr = new StringReader(File.ReadAllText(ctx.assetPath)))
            {
                var builder = new StringBuilder();
                string line;
                string name = "";
                bool deleteEmptyLine = false;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("#T#"))
                    {
                        if (builder.Length > 0 && !string.IsNullOrWhiteSpace(name))
                            SaveSubAsset(ctx, subAsset, builder, name);
                        
                        builder = new StringBuilder();
                        name = line.Replace("#T#", "").Trim();
                        continue;
                    }

                    if (string.IsNullOrEmpty(line))
                    {
                        if (deleteEmptyLine)
                            continue;
                        deleteEmptyLine = true;
                    }
                    else
                    {
                        deleteEmptyLine = false;
                    }

                    builder.AppendLine(line);
                }
                
                if (builder.Length > 0 && !string.IsNullOrWhiteSpace(name))
                    SaveSubAsset(ctx, subAsset, builder, name);
            }
            
            
            //Texture2D icon = Resources.Load<Texture2D>("Editor/Icons/Icon");
            ctx.AddObjectToAsset("Collection", subAsset/*, icon*/); //TODO: add asset icon here
            ctx.SetMainObject(subAsset);
        }

        private static void SaveSubAsset(AssetImportContext ctx, TemplateCollectionAsset asset, StringBuilder builder, string name)
        {
            var templateAsset = ScriptableObject.CreateInstance<TemplateAsset>();
            templateAsset.Template = builder.ToString();
            templateAsset.name = name;
            ctx.AddObjectToAsset(name, templateAsset /*, icon*/); //TODO: add asset icon here
            asset.Templates.Add(templateAsset);
        }

        public override bool SupportsRemappedAssetType(Type type)
        {
            return type.IsAssignableFrom(typeof(TemplateAsset));
        }
    }
}