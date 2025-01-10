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
            "The plugin is focused on the structural form-finding method that simulates the hanging chains. " +
            "Perhaps the most famous example of this method is Antonio Gaudi's hanging chain model of the Sagrada Familia. " +
            "This plugin provides not only methods to construct different types of anchors, loads and chains, but " +
            "additional components to extract and analyze data and generate 3D forms.";

        public override Guid Id => new Guid("2fbfe62c-6e25-4fcd-9107-f8c7f1225c94");

        //Return a string identifying you or your company.
        public override string AuthorName => "Algorithmic Architecture; Ognjen Graovac";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "Website; https://algorithmic-architecture.com/en/home/; " +
            "LinkedIn: https://www.linkedin.com/company/algorithmic-architecture; " +
            "Instagram: https://www.instagram.com/algorithmic_architects/; " +
            "Email: office@algorithmic-architecture.com, graovac@algorithmic-architecture.com;";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => "1.3.0"; //GetType().Assembly.GetName().Version.ToString();
        public override string Version => "1.3.0";
    }
}