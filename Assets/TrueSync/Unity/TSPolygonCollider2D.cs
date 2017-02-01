using System.Collections.Generic;
using UnityEngine;

namespace TrueSync {
    /**
     *  @brief Collider with a polygon 2D shape. 
     **/
    [AddComponentMenu("TrueSync/Physics/PolygonCollider2D", 0)]
    public class TSPolygonCollider2D : TSCollider2D {

        [SerializeField]
        private TSVector2[] _points;

        public TSVector2[] points {
            get {
                if (_body != null) {
                    Physics2D.PolygonShape polygonShape = (Physics2D.PolygonShape)_body.FixtureList[0].Shape;
                    return polygonShape.Vertices.ToArray();
                }

                return _points;
            }

            set {
                _points = value;

                if (_body != null) {
                    Physics2D.PolygonShape polygonShape = (Physics2D.PolygonShape)_body.FixtureList[0].Shape;
                    polygonShape.Vertices = new Physics2D.Vertices(value);
                }
            }
        }

        /**
         *  @brief Create the internal shape used to represent a TSBoxCollider.
         **/
        public override TrueSync.Physics2D.Shape CreateShape() {
            if (_points == null || _points.Length == 0) {
                return null;
            }

            TSVector2 lossy2D = new TSVector2(lossyScale.x, lossyScale.y);

            TrueSync.Physics2D.Vertices v = new Physics2D.Vertices();
            for (int index = 0, length = _points.Length; index < length; index++) {
                v.Add(TSVector2.Scale(_points[index], lossy2D));
            }

            return new TrueSync.Physics2D.PolygonShape(v, 1);
        }

        protected override void DrawGizmos() {
            TSVector2[] allPoints = _points;

            if (allPoints == null || allPoints.Length == 0) {
                return;
            }

            for (int index = 0, length = allPoints.Length - 1; index < length; index++) {
                Gizmos.DrawLine(allPoints[index].ToVector(), allPoints[index+1].ToVector());
            }

            Gizmos.DrawLine(allPoints[allPoints.Length - 1].ToVector(), allPoints[0].ToVector());
        }

        protected override Vector3 GetGizmosSize() {
            return lossyScale.ToVector();
        }

    }

}