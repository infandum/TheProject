/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine.Rendering;
using Collider = Unity.Physics.Collider;


public class Testing : MonoBehaviour {

    [SerializeField] private bool _isDynamic;
    [SerializeField] private RenderMesh _renderMesh;
    [SerializeField] private int _instances = 100;
    public bool IsDynamic => _isDynamic;


    [SerializeField] private int _count;
    private void Start() {
        

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            _count += SpawnGroup();
        }
    }

    private int SpawnGroup()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        for (var i = 0; i<_instances; i++) 
        {
        var position = new float3() { x = UnityEngine.Random.Range(-2.5f, 2.5f), y = UnityEngine.Random.Range(2f, 20f), z = UnityEngine.Random.Range(-2.5f, 2.5f) };
        CreateDynamicSphere(entityManager, _renderMesh, 1, position, quaternion.identity);
        }

        return _instances;
    }

    public Entity CreateDynamicSphere(EntityManager entityManager, RenderMesh displayMesh, float radius, float3 position, quaternion orientation)
    {
        // Sphere with default filter and material. Add to Create() call if you want non default:
        var spCollider = Unity.Physics.SphereCollider.Create( new SphereGeometry(){Center = new float3(){xyz = 0.0f}, Radius = 0.5f});
        return CreateBody(entityManager, displayMesh, position, orientation, spCollider, float3.zero, float3.zero, 1.0f, true);
    }

    public unsafe Entity CreateBody(EntityManager entityManager, RenderMesh displayMesh, float3 position, quaternion orientation, BlobAssetReference<Collider> spCollider, float3 linearVelocity, float3 angularVelocity, float mass, bool isDynamic)
    {
        var componentTypes = new ComponentType[_isDynamic ? 10 : 7];

        componentTypes[0] = typeof(RenderMesh);
        componentTypes[1] = typeof(RenderBounds);
        componentTypes[2] = typeof(Translation);
        componentTypes[3] = typeof(Rotation);
        componentTypes[4] = typeof(LocalToWorld);
        componentTypes[5] = typeof(PhysicsCollider);
        componentTypes[6] = typeof(DestroyEntityDelayed);
        if (_isDynamic)
        {
            componentTypes[7] = typeof(PhysicsVelocity);
            componentTypes[8] = typeof(PhysicsMass);
            componentTypes[9] = typeof(PhysicsDamping);
        }

        var entity = entityManager.CreateEntity(componentTypes);

        entityManager.SetSharedComponentData(entity, displayMesh);
       // entityManager.SetComponentData(entity, new RenderMesh { Value = displayMesh.mesh.bounds.ToAABB() });
        entityManager.SetComponentData(entity, new RenderBounds { Value = displayMesh.mesh.bounds.ToAABB() });

        entityManager.SetComponentData(entity, new Translation { Value = position });
        entityManager.SetComponentData(entity, new Rotation { Value = orientation });

        entityManager.SetComponentData(entity, new PhysicsCollider { Value = spCollider });
        entityManager.SetComponentData(entity, new DestroyEntityDelayed { timeLeft = UnityEngine.Random.Range(2.0f, 20.0f)});
        if (!_isDynamic) return entity;
        
        var colliderPtr = (Collider*)spCollider.GetUnsafePtr();
        entityManager.SetComponentData(entity, PhysicsMass.CreateDynamic(colliderPtr->MassProperties, mass));
        
        // Calculate the angular velocity in local space from rotation and world angular velocity
        var angularVelocityLocal = math.mul(math.inverse(colliderPtr->MassProperties.MassDistribution.Transform.rot), angularVelocity);
        entityManager.SetComponentData(entity, new PhysicsVelocity()
        {
            Linear = linearVelocity,
            Angular = angularVelocityLocal
        });
        entityManager.SetComponentData(entity, new PhysicsDamping()
        {
            Linear = 0.01f,
            Angular = 0.05f
        });

        return entity;
    }
}