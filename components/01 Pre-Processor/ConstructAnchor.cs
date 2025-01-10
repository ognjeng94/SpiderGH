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

    public class ConstructAnchor : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ConstructAnchor class.
        /// </summary>
        public ConstructAnchor()
          : base("Construct Anchor", "Anchor",
              "Construct Anchor. You need to restrict translations at least for one of the axis. ",
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
            pManager.AddPointParameter("Anchor", "Pt", "Anchor Point", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance Tolerance", "dTol", "Search tolerance to the closest Chain", GH_ParamAccess.item, 0.01);
            pManager.AddBooleanParameter("X Translation", "X", "Is translation allowed along X axis", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Y Translation", "Y", "Is translation allowed along Y axis", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Z Translation", "Z", "Is translation allowed along Z axis", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Anchor", "AN", "Anchor data", GH_ParamAccess.item);
            pManager[0].DataMapping = GH_DataMapping.Flatten;

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            #region INPUT DATA 

            //double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            Point3d pt = Point3d.Unset;
            DA.GetData(0, ref pt);

            double distTol = 0.01;
            DA.GetData(1, ref distTol);

            bool transX = false;
            DA.GetData(2, ref transX);

            bool transY = false;
            DA.GetData(3, ref transY);

            bool transZ = false;
            DA.GetData(4, ref transZ);

            #endregion

            #region CHECK DATA

            if (pt == Point3d.Unset || !pt.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Point is not valid");
                return;
            }

            if (distTol < 0)
                distTol = Math.Abs(distTol);

            if (distTol < 1e-5)
                distTol = 1e-5;

            if (transX && transY && transZ)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "You need to restrict movement along one axis at least. Setting 'Z = false'");
                transZ = false;
            }

            #endregion

            S_Anchor anchor = new S_Anchor(pt, distTol, transX, transY, transZ);

            DA.SetData(0, anchor);
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
                return SpiderGH.Properties.Resources._01_ConstructAnchor;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("af274f68-7270-4522-955e-480d69481be7"); }
        }
    }



    #region old
    /*
    public class ConstructAnchor : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ConstructAnchor class.
        /// </summary>
        public ConstructAnchor()
          : base("Construct Anchor", "Anchor",
              "Construct Anchor. You need to restrict translations at least for one of the axis. ",
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
            pManager.AddPointParameter("Anchor", "Pt", "Anchor Point", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance Tolerance", "dTol", "Search tolerance to the closest Chain", GH_ParamAccess.item, 0.01);
            pManager.AddBooleanParameter("X Translation", "X", "Is translation allowed along X axis", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Y Translation", "Y", "Is translation allowed along Y axis", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Z Translation", "Z", "Is translation allowed along Z axis", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Anchor", "AN", "Anchor data", GH_ParamAccess.item);
            pManager[0].DataMapping = GH_DataMapping.Flatten;

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            #region INPUT DATA 

            //double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            Point3d pt = Point3d.Unset;
            DA.GetData(0, ref pt);

            double distTol = 0.01;
            DA.GetData(1, ref distTol);

            bool transX = false;
            DA.GetData(2, ref transX);

            bool transY = false;
            DA.GetData(3, ref transY);

            bool transZ = false;
            DA.GetData(4, ref transZ);

            #endregion

            #region CHECK DATA

            if (pt == Point3d.Unset || !pt.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Point is not valid");
                return;
            }

            if (distTol < 0)
                distTol = Math.Abs(distTol);

            if (distTol < 1e-5)
                distTol = 1e-5;

            if (transX && transY && transZ)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "You need to restrict movement along one axis at least. Setting 'Z = false'");
                transZ = false;
            }

            #endregion

            S_Anchor anchor = new S_Anchor(pt, distTol, transX, transY, transZ);

            DA.SetData(0, anchor);

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
                return SpiderGH.Properties.Resources._01_ConstructAnchor;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("af284f68-7270-4522-955e-480d69481be7"); }
        }
    }
    */
    #endregion
}