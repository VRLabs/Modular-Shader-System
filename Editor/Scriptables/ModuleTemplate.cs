using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    [Serializable]
    public class ModuleTemplate 
    {
        public string Keyword;
        public TemplateAsset Template;
        public bool IsCGOnly = true;
        public List<string> TemplateKeywords;
    }
}