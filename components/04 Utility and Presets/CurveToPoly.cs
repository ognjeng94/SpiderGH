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
    public class CurveToPoly : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CurveToPoly class.
        /// </summary>
        public CurveToPoly()
          : base("Convert Curve to Polyline", "CrvToPoly",
              "Convert Curve to Polyline",
              Universal.Category(), Universal.SubCategory_UtilityPresets())
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
            pManager.AddCurveParameter("Curve", "C", "Input Curve", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance", "d", "Approximate distance between points", GH_ParamAccess.item, 1);
            pManager.AddBooleanParameter("Detect segments", "s", "Detect segments of the curve", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "Converted Polyline", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //GET INPUT DATA

            Curve crv = null;
            DA.GetData(0, ref crv);

            double d = 1;
            DA.GetData(1, ref d);

            bool detect = true;
            DA.GetData(2, ref detect);

            //TEST INPUT DATA

            if (crv == null || !crv.IsValid || crv.GetLength() < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid or too short curve");
                return;
            }
                        
            if (double.IsNaN(d))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid distance parameter");
                return;
            }

            //SOLVER

            bool test = S_Solver.ConvertCurveToPolyline(crv, d, detect, out Polyline poly);

            //OUTPUT DATA

            DA.SetData(0, new GH_Curve(poly.ToNurbsCurve()));
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
                return Properties.Resources._04_ConvertCurveToPoly;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9B37B371-F7AB-4204-A4ED-94AB3B885D83"); }
        }
    }
}