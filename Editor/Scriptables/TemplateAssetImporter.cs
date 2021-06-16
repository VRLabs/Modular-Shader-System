using System;
using System.IO;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    [ScriptedImporter(1, "stemplate")]
    public class TemplateAssetImporter : ScriptedImporter
    { 
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var subAsset = ScriptableObject.CreateInstance<TemplateAsset>();
            subAsset.Template = File.ReadAllText(ctx.assetPath);
            //Texture2D icon = Resources.Load<Texture2D>("Editor/Icons/Icon");
            ctx.AddObjectToAsset("stemplate", subAsset/*, icon*/);
            ctx.SetMainObject(subAsset);
        }

        public override bool SupportsRemappedAssetType(Type type)
        {
            return type.IsAssignableFrom(typeof(TemplateAsset));
        }
    }
}