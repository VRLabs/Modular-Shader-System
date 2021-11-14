using System;
using System.IO;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    [ScriptedImporter(1, MSSConstants.TEMPLATE_EXTENSION)]
    public class TemplateAssetImporter : ScriptedImporter
    { 
        //TODO: add icon
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var subAsset = ScriptableObject.CreateInstance<TemplateAsset>();
            subAsset.TemplateInstanceID = Guid.NewGuid().ToString();
            subAsset.Template = File.ReadAllText(ctx.assetPath);
            //Texture2D icon = Resources.Load<Texture2D>("Editor/Icons/Icon");
            ctx.AddObjectToAsset("Template", subAsset/*, icon*/);
            ctx.SetMainObject(subAsset);
        }

        public override bool SupportsRemappedAssetType(Type type)
        {
            return type.IsAssignableFrom(typeof(TemplateAsset));
        }
    }
}