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
    public class ConstructLinearLoad : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ConstructLinearLoad class.
        /// </summary>
        public ConstructLinearLoad()
          : base("Construct Linear Load", "Ln Load",
              "Construct linear load. Total mass will be distributed equally to the nodes near the line.",
              Universal.Category(), Universal.SubCategory_PreProcessor())
        {
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Line Load", "Ln", "Line Load", GH_ParamAccess.item);
            pManager.AddNumberParameter("Mass/m", "kg/m", "Mass [kg] per meter", GH_ParamAccess.item, 0.75);
            pManager.AddNumberParameter("Distance Tolerance", "dTol", "Distance Tolerance", GH_ParamAccess.item, 0.1);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Load", "LD", "Load data", GH_ParamAccess.item);
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

            Line line = Line.Unset;
            DA.GetData(0, ref line);

            double mass = 10;
            DA.GetData(1, ref mass);

            double distTol = 0.01;
            DA.GetData(2, ref distTol);

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

            if (distTol < 0)
                distTol = Math.Abs(distTol);

            if (distTol < 1e-5)
                distTol = 1e-5;

            if (mass < 0)
                mass = Math.Abs(mass);

            if (mass < tolerance * 10)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Mass is too small");
            }

            #endregion

            S_Load load = new S_Load(line, distTol, mass);

            DA.SetData(0, load);

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
                return SpiderGH.Properties.Resources._01_ConstructLinearLoad;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("99da1824-8c9e-45b4-a836-d6e14c3cb40b"); }
        }
    }
}