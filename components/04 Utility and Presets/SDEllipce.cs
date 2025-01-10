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
    public class SDEllipce : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SDEllipce class.
        /// </summary>
        public SDEllipce()
          : base("SD Ellipse", "SD Ellipse",
              "Subdivision Ellipse",
              Universal.Category(), Universal.SubCategory_UtilityPresets())
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
            pManager.AddPlaneParameter("Plane", "Pl", "Plane", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddIntegerParameter("Sides", "N", "Number of sides", GH_ParamAccess.item, 12);
            pManager.AddNumberParameter("Radius 1", "r1", "Radius 1", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("Radius 2", "r2", "Radius 2", GH_ParamAccess.item, 20);
            pManager.AddIntegerParameter("Subdivision", "SD", "Subdivision level [0-3]", GH_ParamAccess.item, 1);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "Poly", "Initial polyline", GH_ParamAccess.item);
            pManager.AddLineParameter("Lines", "Ln", "Lines", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh", "M", "Mesh", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //INPUT DATA
            Plane plane = Plane.WorldXY;
            DA.GetData(0, ref plane);

            int sides = 12;
            DA.GetData(1, ref sides);

            double r1 = 10;
            DA.GetData(2, ref r1);

            double r2 = 20;
            DA.GetData(3, ref r2);

            int subdiv = 1;
            DA.GetData(4, ref subdiv);

            //TEST INPUT DATA

            if (plane == Plane.Unset)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid plane");
                return;
            }

            if (sides < 3) sides = 3;
            if (sides > 100) sides = 100;

            if (r1 < 0) r1 = Math.Abs(r1);
            if (r1 < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
                r1 = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10;

            if (r2 < 0) r2 = Math.Abs(r2);
            if (r2 < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
                r2 = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10;

            if (subdiv < 0) subdiv = 0;
            if (subdiv > 3) subdiv = 3;

            //SOLVER

            bool testPoly = S_Solver.ConstructPolyEllipse(plane, sides, r1, r2, out Polyline poly);

            if (!testPoly)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid polyline");
                return;
            }

            bool testMesh = S_Solver.SimplePolylineToMesh(poly, plane.Origin, out Mesh meshS);

            if (!testMesh)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid mesh");
                return;
            }

            List<Line> edges = new List<Line>();
            Mesh finalMesh = new Mesh();

            if (subdiv == 0)
            {
                bool testEdges = S_Solver.GetMeshEdges(meshS, out edges);
                finalMesh = meshS.DuplicateMesh();

                if (!testEdges)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid edges");
                    return;
                }
            }
            else
            {
                for (int i = 0; i < subdiv; i++)
                {
                    bool testSubD = S_Solver.SingleSubdivideMesh(meshS, out Mesh meshE);

                    if (!testSubD)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid subdivision");
                        return;
                    }

                    meshS = meshE.DuplicateMesh();
                }

                bool testEdges = S_Solver.GetMeshEdges(meshS, out edges);
                finalMesh = meshS.DuplicateMesh();

                if (!testEdges)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid edges");
                    return;
                }
            }

            //OUTPUT DATA
            DA.SetData(0, poly);
            DA.SetDataList(1, edges);
            DA.SetData(2, meshS);

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
                return SpiderGH.Properties.Resources._04_SDEllipse;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("A5D96855-3071-42DA-8408-A4FBF7003A51"); }
        }
    }
}