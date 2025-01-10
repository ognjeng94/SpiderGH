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
    public class PointsOnClosestCurves : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PointsOnClosestCurves class.
        /// </summary>
        public PointsOnClosestCurves()
          : base("Points on Closest Curves", "Pts on Crvs",
              "Points on the closest curves",
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
            pManager.AddPointParameter("Points", "Pts", "Points to pull", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curves", "Crvs", "Curves to pull points on to", GH_ParamAccess.list);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Pts", "Closest points on the curves", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Indices", "i", "Indices of the closest curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Distances", "d", "Distances to the closest curves", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //GET INPUT DATA

            List<Point3d> inputPts = new List<Point3d>();
            DA.GetDataList(0, inputPts);

            List<Curve> inputCrvs = new List<Curve>();
            DA.GetDataList(1, inputCrvs);

            //TEST INPUT DATA

            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            List<Curve> crvs = new List<Curve>();
            Point3dList pts = new Point3dList();

            foreach (var crv in inputCrvs)
                if (crv != null && crv.IsValid && crv.GetLength() > tolerance)
                    crvs.Add(crv);

            foreach (var pt in inputPts)
                if (pt != Point3d.Unset && pt.IsValid)
                    pts.Add(pt);

            if (pts == null || pts.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough valid points");
                return;
            }

            if (crvs == null || crvs.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not enough valid curves");
                return;
            }

            //SOLVER

            bool test = S_Solver.PointsOnClosestCurves(pts, crvs,
                out List<Point3d> pts2, out List<int> indices, out List<double> distances);

            //OUTPUT DATA

            DA.SetDataList(0, pts2);
            DA.SetDataList(1, indices);
            DA.SetDataList(2, distances);

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
                return SpiderGH.Properties.Resources._04_PointsOnClosestCurves;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("D71C19EB-DBEB-42B9-B0A9-DBCB740F8C2C"); }
        }
    }
}