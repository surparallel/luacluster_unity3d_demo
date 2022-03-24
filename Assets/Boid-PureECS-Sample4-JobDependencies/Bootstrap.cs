using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;

namespace Boid.PureECS.Sample4
{

public class Bootstrap : MonoBehaviour 
{
    public static Bootstrap Instance 
    { 
        get; 
        private set; 
    }

    public static Param Param
    {
        get { return Instance.param; }
    }

    [SerializeField]
    Vector3 boidScale = new Vector3(0.1f, 0.1f, 0.3f);

    [SerializeField]
    Param param;

    [SerializeField]
    MeshInstanceRenderer renderer;

        public Entity Create()
        {
            var manager = World.Active.GetOrCreateManager<EntityManager>();
            var archetype = manager.CreateArchetype(
                typeof(Position),
                typeof(Rotation),
                typeof(Scale),
                typeof(Velocity),
                typeof(Origin),
                typeof(MeshInstanceRenderer));
            var random = new Unity.Mathematics.Random(853);


            var entity = manager.CreateEntity(archetype);
            manager.SetComponentData(entity, new Position { Value = float3.zero });
            manager.SetComponentData(entity, new Rotation { Value = quaternion.identity });
            manager.SetComponentData(entity, new Scale { Value = new float3(boidScale.x, boidScale.y, boidScale.z) });
            manager.SetComponentData(entity, new Velocity { Value = 0 });
            manager.SetComponentData(entity, new Origin { Value = float3.zero });
            manager.SetSharedComponentData(entity, renderer);

            return entity;
        }
        public void DestroyEntity(Entity entity)
        {
            var manager = World.Active.GetOrCreateManager<EntityManager>();
            manager.DestroyEntity(entity);
        }

        public void CompleteAllJobs()
        {
            var manager = World.Active.GetOrCreateManager<EntityManager>();
            manager.CompleteAllJobs();
        }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
        }

        void OnDrawGizmos()
        {
            if (!param) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(this.transform.position, Vector3.one * param.wallScale);
        }
    }
}