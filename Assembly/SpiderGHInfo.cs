using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace SpiderGH
{
    public class SpiderGHInfo : GH_AssemblyInfo
    {
        public override string Name => "SpiderGH";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Properties.Resources.Icon_24;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Spider is a plugin for Grasshopper written in C#. " +
                    "The plugin is focused on the structural form-finding method that simulates the hanging chains." +
                    "Perhaps the most famous example of this method is Antonio Gaudi's hanging chain model of the Sagrada Familia." +
                    "This plugin provides not only methods to construct different types of anchors, loads and chains, but " +
                    "additional components to extract and analyze data and generate 3D forms.";

        public override Guid Id => new Guid("2114500F-F81B-4A5F-B580-FC961E2B80F9");

        //Return a string identifying you or your company.
        public override string AuthorName => "Ognjen Graovac";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "e-mail: arch.graovac@gmail.com, ognjeng94@gmail.com; " +
                    "linkedin: https://www.linkedin.com/in/ognjen-graovac-1b27a513a/; " +
                    "instagram: https://www.instagram.com/ognjen_graovac/?hl=sr; " +
                    "location: Belgrade, Serbia;";

        public override string Version => "1.2.1";
    }
}