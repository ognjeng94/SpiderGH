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
    public class FlipLineDirection_Plane : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the FlipLineDirection_Plane class.
        /// </summary>
        public FlipLineDirection_Plane()
          : base("Flip Line Direction - Plane", "FlipLine - Pln",
              "Flip line direction with the plane",
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
            pManager.AddPlaneParameter("Plane Guide", "P", "Plane guide", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddBooleanParameter("Positive local X", "x", "Orient line towards positive/negative local X axis", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Positive local Y", "y", "Orient line towards positive/negative local Y axis", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Positive local Z", "z", "Orient line towards positive/negative local Z axis", GH_ParamAccess.item, true);
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

            Plane plane = Plane.Unset;
            DA.GetData(1, ref plane);

            bool posX, posY, posZ;
            posX = posY = posZ = true;

            DA.GetData(2, ref posX);
            DA.GetData(3, ref posY);
            DA.GetData(4, ref posZ);


            //TEST INPUT DATA

            if (inputLine == Line.Unset || !inputLine.IsValid || inputLine.Length < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid or too short line");
                return;
            }

            if (plane == Plane.Unset || !plane.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid gudie plane");
                return;
            }

            //SOLVER

            Line newline = S_Solver.FlipLineDirection(inputLine, plane, posX, posY, posZ, out bool flipResult);

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
                return Properties.Resources._04_FlipLine_Plane;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("CFCAED3C-A2A2-45D1-ABE3-9FD60C71DAA9"); }
        }
    }
}