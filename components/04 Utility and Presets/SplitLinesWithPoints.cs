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
    public class SplitLinesWithPoints : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SplitLinesWithPoints class.
        /// </summary>
        public SplitLinesWithPoints()
          : base("Split Lines With Points", "Split Ln",
              "Split Lines With Points",
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
            pManager.AddLineParameter("Lines", "Ln", "Lines to split", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "Pt", "Points to split with", GH_ParamAccess.list);
            pManager.AddNumberParameter("Distance", "dT", "Distance tolerance to the closest line", GH_ParamAccess.item, 1e-3);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Lines", "Ln", "Lines", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Indices", "i", "Indices of the original lines", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //GET INPUT DATA

            List<Line> inputLines = new List<Line>();
            DA.GetDataList(0, inputLines);

            List<Point3d> inputPts = new List<Point3d>();
            DA.GetDataList(1, inputPts);

            double distance = 1e-3;
            DA.GetData(2, ref distance);

            //TEST INPUT DATA

            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            List<Line> lns = new List<Line>();
            Point3dList pts = new Point3dList();

            foreach (var ln in inputLines)
                if (ln != Line.Unset && ln.IsValid && ln.Length > tolerance)
                    lns.Add(ln);

            foreach (var pt in inputPts)
                if (pt != Point3d.Unset && pt.IsValid)
                    pts.Add(pt);

            if (pts == null || pts.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough valid points");
                return;
            }

            if (lns == null || lns.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough valid curves");
                return;
            }

            //SOLVER

            List<Line> newLines = new List<Line>();
            List<int> oldIndices = new List<int>();

            for (int i = 0; i < lns.Count; i++)
            {
                bool test = S_Solver.SplitLineCurve(new LineCurve(lns[i]), pts, distance, tolerance * 10, out List<LineCurve> segments);

                if (test && segments != null && segments.Count > 0)
                {
                    foreach (var segment in segments)
                    {
                        newLines.Add(segment.Line);
                        oldIndices.Add(i);
                    }
                }
                else
                {
                    newLines.Add(lns[i]);
                    oldIndices.Add(i);
                }
            }

            //OUTPUT DATA

            DA.SetDataList(0, newLines);
            DA.SetDataList(1, oldIndices);

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
                return SpiderGH.Properties.Resources._04_SplitLinesWithPoints;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0E0B5963-6E9C-47EE-ABA6-314A88274AF0"); }
        }
    }
}