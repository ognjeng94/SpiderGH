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
    public class ConstructRegionLoad : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ConstructRegionLoad class.
        /// </summary>
        public ConstructRegionLoad()
          : base("Construct Region Load", "Brp Load",
              "Construct region load. Total mass will be distributed equally to the nodes inside brep.",
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
            pManager.AddBrepParameter("Region Load", "Brep", "Region Load as brep", GH_ParamAccess.item);
            pManager.AddNumberParameter("Mass", "kg", "Mass [kg]", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("Distance Tolerance", "dTol", "Distance tolerance", GH_ParamAccess.item, 0.1);

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

            Brep brep = new Brep();
            DA.GetData(0, ref brep);

            double mass = 10;
            DA.GetData(1, ref mass);

            double distTol = 0.01;
            DA.GetData(2, ref distTol);

            #endregion

            #region CHECK DATA

            if (brep == null || !brep.IsValid || brep.Vertices.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Brep is not valid");
                return;
            }

            if (mass < 0)
                mass = Math.Abs(mass);

            if (mass < tolerance * 10)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Mass is too small");
            }

            #endregion

            S_Load load = new S_Load(brep, distTol, mass);

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
                return SpiderGH.Properties.Resources._01_ConstructRegionLoad;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d9a7257d-822d-4a21-854d-3f8b7b614580"); }
        }
    }
}