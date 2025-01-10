using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Rhino;
using Rhino.Collections;
using Rhino.Input.Custom;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.DocObjects;
using Rhino.Geometry.Intersect;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SpiderGH.components;

namespace SpiderGH
{
    public class ConstructChain : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ConstructChain class.
        /// </summary>
        public ConstructChain()
          : base("Construct Chain", "Chain",
              "Construct chain",
              Universal.Category(), Universal.SubCategory_PreProcessor())
        {
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Line", "ln", "Initial line of the chain", GH_ParamAccess.item);
            pManager.AddNumberParameter("Elasticity", "k", "Elasticity [0.01-10]. Smaller values = more deformation", GH_ParamAccess.item, 0.75);
            pManager.AddNumberParameter("Mass/m", "kg/m", "Mass [kg] per meter", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("Node-Resolution", "resN", "The average distance of the nodes on the initial line. Smaller number = more nodes", GH_ParamAccess.item, 0.25);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Chain", "CH", "Chain data", GH_ParamAccess.item);
            pManager[0].DataMapping = GH_DataMapping.Flatten;

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            #region INPUT DATA 

            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            double minElasticity = 0.01;
            double maxElasticity = 10;

            Line line = Line.Unset;
            DA.GetData(0, ref line);

            double elasticity = 0.75;
            DA.GetData(1, ref elasticity);

            double massPerM = 0.5;
            DA.GetData(2, ref massPerM);

            double nodeRes = 0.25;
            DA.GetData(3, ref nodeRes);



            #endregion

            #region CHECK DATA

            if (line == Line.Unset || !line.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Line is not valid");
                return;
            }

            if (line.Length < tolerance * 10)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Line is too short");
            }

            if (elasticity < 0)
                elasticity = Math.Abs(elasticity);

            if (elasticity < minElasticity)
                elasticity = minElasticity;

            if (elasticity > maxElasticity)
                elasticity = maxElasticity;

            if (massPerM < 0)
                massPerM = Math.Abs(massPerM);

            if (nodeRes < 0)
                nodeRes = Math.Abs(nodeRes);

            #endregion


            S_Chain chain = new S_Chain(line, elasticity, massPerM, nodeRes);

            DA.SetData(0, chain);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources._01_ConstructChain;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3828307d-f8b4-4465-b045-58436638c8bf"); }
        }
    }
}