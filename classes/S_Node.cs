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

namespace SpiderGH
{
    public class S_Node
    {
        #region PROPERTIES


        public int Index { get; set; } //metadata
        public Guid ID { get; set; }
        public bool IsValid { get; set; }


        public Point3d Position { get; set; } //geometry
        public Point3d InitialPosition { get; set; }

        public Vector3d Force { get; set; } //node data
        public double Mass { get; set; }
        public double Load { get; set; }

        public List<S_Node> Neighbours { get; set; } //connectivity
        public List<S_Chain> NeighbourChains { get; set; }

        public bool IsAnchor { get; set; } //anchor data
        public bool MoveX { get; set; }
        public bool MoveY { get; set; }
        public bool MoveZ { get; set; }

        public int SimulationPhase { get; set; } //-1 invalid, 0 pre-processor, 1 processor, 2 post-processor


        #endregion

        #region CONSTRUCTOR

        public S_Node(Point3d pt, bool isAnchor, bool x, bool y, bool z)
        {
            Index = -1;
            ID = Guid.NewGuid();

            if (pt != Point3d.Unset && pt.IsValid)
            {
                Position = new Point3d(pt);
                InitialPosition = new Point3d(pt);
                IsValid = true;
            }
            else
            {
                Position = Point3d.Unset;
                InitialPosition = Point3d.Unset;
                IsValid = false;
            }

            Force = Vector3d.Unset;
            Mass = 0;
            Load = 0;

            Neighbours = new List<S_Node>();
            NeighbourChains = new List<S_Chain>();

            IsAnchor = isAnchor;
            MoveX = x;
            MoveY = y;
            MoveZ = z;

            SimulationPhase = 1;
        }


        #endregion

        #region METHODS

        public static void UpdateNodeIndices(List<S_Node> allNodes)
        {
            for (int i = 0; i < allNodes.Count; i++)
                allNodes[i].Index = i;
        }

        public void CalculateForces(Vector3d gravity, double friction, int substeps, double maxAmplitude)
        {
            #region Forces
            //calculate the forces for nodes and achors

            //gravity
            Vector3d gravityForce = (Mass + Load) * gravity * 0.1;


            //spring forces
            List<Vector3d> springForces = new List<Vector3d>();

            for (int i = 0; i < Neighbours.Count; i++)
            {
                // old //Vector3d springF = NeighbourChains[i].Elasticity * new Vector3d(Neighbours[i].Position - Position);

                double elasticity = NeighbourChains[i].PolyLength / NeighbourChains[i].MaxLength;
                Vector3d springF = elasticity * new Vector3d(Neighbours[i].Position - Position);

                springForces.Add(springF);
            }

            //total force
            Vector3d totalForce = gravityForce;
            foreach (Vector3d vec in springForces)
                totalForce += vec;

            totalForce *= friction;

            if (totalForce.Length > maxAmplitude)
            {
                bool test = totalForce.Unitize();
                if (test)
                    totalForce *= maxAmplitude;
            }

            //dividing the force with the desired substeps in order to make the movement smaller and the simulation more precise
            totalForce /= substeps;

            #endregion


            #region Anchors
            //restrict movement for anchors

            if (IsAnchor)
            {
                if (!MoveX && !MoveY && !MoveZ)
                    totalForce = Vector3d.Zero;
                else if (MoveX && !MoveY && !MoveZ)
                    totalForce = new Vector3d(totalForce.X, 0, 0);
                else if (!MoveX && MoveY && !MoveZ)
                    totalForce = new Vector3d(0, totalForce.Y, 0);
                else if (!MoveX && !MoveY && MoveZ)
                    totalForce = new Vector3d(0, 0, totalForce.Z);
                else if (MoveX && MoveY && !MoveZ)
                    totalForce = new Vector3d(totalForce.X, totalForce.Y, 0);
                else if (MoveX && !MoveY && MoveZ)
                    totalForce = new Vector3d(totalForce.X, 0, totalForce.Z);
                else if (!MoveX && MoveY && MoveZ)
                    totalForce = new Vector3d(0, totalForce.Y, totalForce.Z);
            }

            #endregion

            //updating the properties
            Force = totalForce;
        }

        public void UpdatePosition()
        {
            Point3d tempPt = Position + Force;
            Position = tempPt;
        }



        public static void GetPositionsAndForces(List<S_Node> allNodes, out Point3dList pos, out List<Vector3d> vecs)
        {
            pos = new Point3dList();
            vecs = new List<Vector3d>();

            foreach (var node in allNodes)
            {
                pos.Add(node.Position);
                vecs.Add(node.Force);
            }
        }

        public static void GetMassAndLoad(List<S_Node> allNodes,
                                          out List<double> mass, out List<double> laod, out List<double> combined)
        {
            mass = new List<double>();
            laod = new List<double>();
            combined = new List<double>();

            foreach (var node in allNodes)
            {
                mass.Add(node.Mass);
                laod.Add(node.Load);
                combined.Add(node.Mass + node.Load);
            }
        }

        public static void MaxDisplacement(List<S_Node> allNodes, out double maxDisplacement)
        {
            maxDisplacement = 0;

            foreach (var node in allNodes)
                if (node.Force.Length > maxDisplacement)
                    maxDisplacement = node.Force.Length;
        }

        public void ResetPositionsAndForces()
        {
            Position = InitialPosition;
            Force = Vector3d.Unset;
        }

        public static void TransformNode(List<S_Node> allNodes, Transform xform)
        {
            foreach (var node in allNodes)
            {
                Point3d pt = node.Position;
                pt.Transform(xform);

                node.Position = pt;
            }
        }

        public static void AddPointLoads(List<S_Node> allNodes, List<S_Load> allLoads)
        {
            if (allLoads == null || allLoads.Count < 1)
                return;

            List<S_Load> pointLoads = new List<S_Load>();
            foreach (var load in allLoads)
                if (load.Type == S_Load.LoadType.PointLoad)
                    pointLoads.Add(load);

            Point3dList pts = new Point3dList();
            foreach (var node in allNodes)
                pts.Add(node.Position);

            foreach (var load in pointLoads)
            {
                int index = pts.ClosestIndex(load.Position);
                double distance = load.Position.DistanceTo(pts[index]);

                if (distance < load.DistanceTolerance)
                {
                    allNodes[index].Load += load.Mass;
                }
            }
        }

        public static void AddLinearLoads(List<S_Node> allNodes, List<S_Load> allLoads)
        {
            if (allLoads == null || allLoads.Count < 1)
                return;

            List<S_Load> linearLoads = new List<S_Load>();
            foreach (var load in allLoads)
                if (load.Type == S_Load.LoadType.LineLoad)
                    linearLoads.Add(load);

            foreach (var load in linearLoads)
            {
                List<S_Node> selectedNodes = new List<S_Node>();
                foreach (var node in allNodes)
                {
                    load.LinePosition.ClosestPoint(node.InitialPosition, out double t);

                    if (node.InitialPosition.DistanceTo(load.LinePosition.PointAt(t)) < load.DistanceTolerance)
                        selectedNodes.Add(node);
                }

                if (selectedNodes.Count > 0)
                {
                    double massPerNode = load.Mass / selectedNodes.Count;
                    foreach (var selNode in selectedNodes)
                        selNode.Load += massPerNode;
                }
            }
        }

        public static void AddRegionLoads(List<S_Node> allNodes, List<S_Load> allLoads)
        {
            if (allLoads == null || allLoads.Count < 1)
                return;

            List<S_Load> regionLoads = new List<S_Load>();
            foreach (var load in allLoads)
                if (load.Type == S_Load.LoadType.BrepLoad)
                    regionLoads.Add(load);

            foreach (var load in regionLoads)
            {
                Brep region = load.BrepPosition.DuplicateBrep();

                List<S_Node> selectedNodes = new List<S_Node>();

                foreach (var node in allNodes)
                {
                    if (region.IsPointInside(node.Position, load.DistanceTolerance, false))
                        selectedNodes.Add(node);
                }

                if (selectedNodes.Count > 0)
                {
                    double massPerNode = load.Mass / selectedNodes.Count;
                    foreach (var selNode in selectedNodes)
                        selNode.Load += massPerNode;
                }
            }
        }

        #endregion
    }
}
