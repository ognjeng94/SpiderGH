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
    public class FlipLineDirection_Point : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the FlipLineDirection class.
        /// </summary>
        public FlipLineDirection_Point()
          : base("Flip Line Direction - Point", "FlipLine - Pt",
              "Flip line direction with the point",
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
            pManager.AddLineParameter("Line", "L", "Line", GH_ParamAccess.item);
            pManager.AddPointParameter("Point Guide", "P", "Point guide", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddBooleanParameter("Towards Point", "T", "If true, line is oriented towards the point.", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Line", "L", "Line", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Flip Result", "F", "If true, the line was flipped. If false, no action was performed.", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //GET INPUT DATA

            Line inputLine = Line.Unset;
            DA.GetData(0, ref inputLine);

            Point3d guidePt = Point3d.Origin;
            DA.GetData(1, ref guidePt);

            bool toward = true;
            DA.GetData(2, ref toward);

            //TEST INPUT DATA

            if (inputLine == Line.Unset || !inputLine.IsValid || inputLine.Length < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid or too short line");
                return;
            }

            if (guidePt == Point3d.Unset || !guidePt.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid guide point");
                return;
            }

            //SOLVER

            Line newline = S_Solver.FlipLineDirection(inputLine, guidePt, toward, out bool flipResult);

            //OUTPUT DATA

            DA.SetData(0, new GH_Line(newline));
            DA.SetData(1, new GH_Boolean(flipResult));

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
                return Properties.Resources._04_FlipLine_Point;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("682A2DD6-ED1D-4726-85E9-ACFB591D2E4F"); }
        }
    }
}