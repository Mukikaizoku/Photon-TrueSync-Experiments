using System;

namespace TrueSync
{

    /**
    *  @brief Helpers for 2D physics.
    **/
    public class TSPhysics2D {

        public enum TSCapsuleDirection2D {

            VERTICAL,

            HORIZONTAL

        }

        private static FP POINT_RADIUS = 0.001f;

        private static object OverlapGeneric(Physics2D.Shape shape, TSVector2 position, Physics2D.BodySpecialSensor sensorType) {
            Physics2D.World world = (Physics2D.World)Physics2DWorldManager.instance.GetWorld();

            Physics2D.Body body = Physics2D.BodyFactory.CreateBody(world);
            body.CreateFixture(shape);

            body.BodyType = Physics2D.BodyType.Static;
            body.IsSensor = true;
            body.CollidesWith = Physics2D.Category.All;

            body.SpecialSensor = sensorType;
            body.Position = position;

            world.RemoveBody(body);
            world.ProcessRemovedBodies();

            if (body._specialSensorResults.Count > 0) {
                if (sensorType == Physics2D.BodySpecialSensor.ActiveOnce) {
                    return Physics2DWorldManager.instance.GetGameObject(body._specialSensorResults[0]).GetComponent<TSCollider2D>();
                } else {
                    TSCollider2D[] result = new TSCollider2D[body._specialSensorResults.Count];
                    for (int i = 0; i < body._specialSensorResults.Count; i++) {
                        result[i] = Physics2DWorldManager.instance.GetGameObject(body._specialSensorResults[i]).GetComponent<TSCollider2D>();
                    }

                    return result;
                }

            }

            return null;
        }

        private static object _OverlapCircle(TSVector2 point, FP radius, Physics2D.BodySpecialSensor sensorType) {
            return OverlapGeneric(new Physics2D.CircleShape(radius, 1), point, sensorType);
        }

        /**
        *  @brief Returns the first {@link TSCollider2D} within a circular area. Returns null if there is none.
        *  
        *  @param point Center of the circle in world space.
        *  @param radius Radius of the circle.
        **/
        public static TSCollider2D OverlapCircle(TSVector2 point, FP radius) {
            return (TSCollider2D) _OverlapCircle(point, radius, Physics2D.BodySpecialSensor.ActiveOnce);
        }

        /**
        *  @brief Returns all {@link TSCollider2D} within a circular area. Returns null if there is none.
        *  
        *  @param point Center of the circle in world space.
        *  @param radius Radius of the circle.
        **/
        public static TSCollider2D[] OverlapCircleAll(TSVector2 point, FP radius) {
            return (TSCollider2D[]) _OverlapCircle(point, radius, Physics2D.BodySpecialSensor.ActiveAll);
        }

        /**
        *  @brief Returns the first {@link TSCollider2D} within a rectangular area. Returns null if there is none.
        *  
        *  @param pointA Top-left corner of the rectangle.
        *  @param radius Bottom-right corner of the rectangle.
        **/
        public static object _OverlapArea(TSVector2 pointA, TSVector2 pointB, Physics2D.BodySpecialSensor sensorType) {
            TSVector2 center;
            center.x = (pointA.x + pointB.x) * FP.Half;
            center.y = (pointA.y + pointB.y) * FP.Half;

            Physics2D.Vertices vertices = new Physics2D.Vertices(4);
            vertices.Add(new TSVector2(pointA.x, pointA.y) - center);
            vertices.Add(new TSVector2(pointB.x, pointA.y) - center);
            vertices.Add(new TSVector2(pointB.x, pointB.y) - center);
            vertices.Add(new TSVector2(pointA.x, pointB.y) - center);

            return OverlapGeneric(new Physics2D.PolygonShape(vertices, 1), center, sensorType);
        }

        /**
        *  @brief Returns the first {@link TSCollider2D} within a rectangular area. Returns null if there is none.
        *  
        *  @param pointA Top-left corner of the rectangle.
        *  @param radius Bottom-right corner of the rectangle.
        **/
        public static TSCollider2D OverlapArea(TSVector2 pointA, TSVector2 pointB) {
            return (TSCollider2D) _OverlapArea(pointA, pointB, Physics2D.BodySpecialSensor.ActiveOnce);
        }

        /**
        *  @brief Returns all {@link TSCollider2D} within a rectangular area. Returns null if there is none.
        *  
        *  @param pointA Top-left corner of the rectangle.
        *  @param radius Bottom-right corner of the rectangle.
        **/
        public static TSCollider2D[] OverlapAreaAll(TSVector2 pointA, TSVector2 pointB) {
            return (TSCollider2D[]) _OverlapArea(pointA, pointB, Physics2D.BodySpecialSensor.ActiveAll);
        }

        /**
        *  @brief Returns the first {@link TSCollider2D} within a small circular area. Returns null if there is none.
        *  
        *  @param point Center of the point in world space.
        **/
        public static TSCollider2D OverlapPoint(TSVector2 point) {
            return (TSCollider2D)_OverlapCircle(point, POINT_RADIUS, Physics2D.BodySpecialSensor.ActiveOnce);
        }

        /**
        *  @brief Returns all {@link TSCollider2D} within a small circular area. Returns null if there is none.
        *  
        *  @param point Center of the point in world space.
        **/
        public static TSCollider2D[] OverlapPointAll(TSVector2 point) {
            return (TSCollider2D[])_OverlapCircle(point, POINT_RADIUS, Physics2D.BodySpecialSensor.ActiveAll);
        }

        private static object _OverlapBox(TSVector2 point, TSVector2 size, FP angle, Physics2D.BodySpecialSensor sensorType) {
            size *= FP.Half;
            angle *= FP.Deg2Rad;

            return OverlapGeneric(new Physics2D.PolygonShape(Physics2D.PolygonTools.CreateRectangle(size.x, size.y, point, angle * -1), 1), point, sensorType);
        }

        /**
        *  @brief Returns the first {@link TSCollider2D} within a box area. Returns null if there is none.
        *  
        *  @param point Center of the box in world space.
        *  @param size Size of the box.
        *  @param angle Rotation angle in degrees of the box.
        **/
        public static TSCollider2D OverlapBox(TSVector2 point, TSVector2 size, FP angle) {
            return (TSCollider2D) _OverlapBox(point, size, angle, Physics2D.BodySpecialSensor.ActiveOnce);
        }

        /**
        *  @brief Returns all {@link TSCollider2D} within a box area. Returns null if there is none.
        *  
        *  @param point Center of the box in world space.
        *  @param size Size of the box.
        *  @param angle Rotation angle in degrees of the box.
        **/
        public static TSCollider2D[] OverlapBoxAll(TSVector2 point, TSVector2 size, FP angle) {
            return (TSCollider2D[]) _OverlapBox(point, size, angle, Physics2D.BodySpecialSensor.ActiveAll);
        }

        private static object _OverlapCapsule(TSVector2 point, TSVector2 size, TSCapsuleDirection2D direction, FP angle, Physics2D.BodySpecialSensor sensorType) {
            if (direction == TSCapsuleDirection2D.HORIZONTAL) {
                FP aux = size.y;
                size.y = size.x;
                size.x = aux;

                angle += 90;
            }

            FP radius = size.x * FP.Half;
            Physics2D.Vertices capVerts = Physics2D.PolygonTools.CreateCapsule(size.y, radius, 8, radius, 8);

            Physics2D.PolygonTools.TransformVertices(capVerts, point, angle * FP.Deg2Rad * -1);

            return OverlapGeneric(new Physics2D.PolygonShape(capVerts, 1), point, sensorType);
        }

        /**
        *  @brief Returns the first {@link TSCollider2D} within a capsule area. Returns null if there is none.
        *  
        *  @param point Center of the capsule in world space.
        *  @param size Size of the capsule.
        *  @param direction If it is a vertical or horizontal capsule.
        *  @param angle Rotation angle in degrees of the capsule.
        **/
        public static TSCollider2D OverlapCapsule(TSVector2 point, TSVector2 size, TSCapsuleDirection2D direction, FP angle) {
            return (TSCollider2D) _OverlapCapsule(point, size, direction, angle, Physics2D.BodySpecialSensor.ActiveOnce);
        }

        /**
        *  @brief Returns all {@link TSCollider2D} within a capsule area. Returns null if there is none.
        *  
        *  @param point Center of the capsule in world space.
        *  @param size Size of the capsule.
        *  @param direction If it is a vertical or horizontal capsule.
        *  @param angle Rotation angle in degrees of the capsule.
        **/
        public static TSCollider2D[] OverlapCapsuleAll(TSVector2 point, TSVector2 size, TSCapsuleDirection2D direction, FP angle) {
            return (TSCollider2D[]) _OverlapCapsule(point, size, direction, angle, Physics2D.BodySpecialSensor.ActiveAll);
        }

        public static object _CircleCast(TSVector2 origin, FP radius, TSVector2 direction, FP distance, Physics2D.BodySpecialSensor sensorType) {
            if (distance + radius > FP.MaxValue) {
                distance = FP.MaxValue - radius;
            }

            direction.Normalize();

            TSVector2 offsetToCenter = ((direction * distance) * FP.Half);
            offsetToCenter.x = FP.Abs(offsetToCenter.x);
            offsetToCenter.y = FP.Abs(offsetToCenter.y);

            FP angle = TSVector2.Angle(direction, TSVector2.right);

            if (direction.x <= 0 && direction.y >= 0) {
                offsetToCenter.x = -offsetToCenter.x;
            } else if (direction.x <= 0 && direction.y <= 0) {
                offsetToCenter.x = -offsetToCenter.x;
                offsetToCenter.y = -offsetToCenter.y;
                angle = -angle;
            } else if (direction.x >= 0 && direction.y <= 0) {
                offsetToCenter.y = -offsetToCenter.y;
                angle = -angle;
            }

            TSVector2 center = origin + offsetToCenter;

            object result = _OverlapCapsule(center, new TSVector2(distance + radius * 2, radius * 2), TSCapsuleDirection2D.HORIZONTAL, -angle, sensorType);

            if (result is TSCollider2D) {
                return new TSRaycastHit2D((TSCollider2D) result);
            } else {
                TSCollider2D[] resultAux = (TSCollider2D[]) result;
                TSRaycastHit2D[] resultHit = new TSRaycastHit2D[resultAux.Length];

                for (int index = 0; index < resultHit.Length; index++) {
                    resultHit[index] = new TSRaycastHit2D(resultAux[index]);
                }

                return resultHit;
            }
        }

        /**
        *  @brief Cast a circle and returns a {@link TSRaycastHit2D} with information about the first {@link TSCollider2D} found. Returns null if there is none.
        *  
        *  @param origin Origin of the circle in world space.
        *  @param radius Radius of the circle.
        *  @param direction Direction {@link TSVector2} of the cast.
        *  @param distance Max distance to reach.
        **/
        public static TSRaycastHit2D CircleCast(TSVector2 origin, FP radius, TSVector2 direction, FP distance) {
            return (TSRaycastHit2D) _CircleCast(origin, radius, direction, distance, Physics2D.BodySpecialSensor.ActiveOnce);
        }

        /**
        *  @brief Cast a circle and returns an array {@link TSRaycastHit2D} with information about all {@link TSCollider2D} found. Returns null if there is none.
        *  
        *  @param origin Origin of the circle in world space.
        *  @param radius Radius of the circle.
        *  @param direction Direction {@link TSVector2} of the cast.
        *  @param distance Max distance to reach.
        **/
        public static TSRaycastHit2D[] CircleCastAll(TSVector2 origin, FP radius, TSVector2 direction, FP distance) {
            return (TSRaycastHit2D[]) _CircleCast(origin, radius, direction, distance, Physics2D.BodySpecialSensor.ActiveAll);
        }

    }

}