using UnityEngine;

namespace MergeCafe.Suika
{
    /// <summary>
    /// One fruit in the watermelon game: a 2D physics body whose collisions with a
    /// same-level fruit are routed to <see cref="SuikaGame.RequestMerge"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class SuikaFruit : MonoBehaviour
    {
        public int Level { get; private set; }
        public bool Merged { get; set; }
        public float DropTime { get; private set; }
        public bool Dropped { get; private set; }

        public Rigidbody2D Body { get; private set; }

        private SuikaGame _game;
        private CircleCollider2D _collider;

        public static SuikaFruit Create(SuikaGame game, int level, Vector2 pos, bool held,
            PhysicsMaterial2D material, Transform parent)
        {
            var go = new GameObject($"Fruit_{level:D2}");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            float scale = SuikaCatalog.Radius(level) / SuikaFruitSprites.SpriteRadius;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SuikaFruitSprites.Fruit(level);
            sr.sortingOrder = 10 + level;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = SuikaFruitSprites.SpriteRadius;
            col.sharedMaterial = material;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var fruit = go.AddComponent<SuikaFruit>();
            fruit.Level = level;
            fruit._game = game;
            fruit.Body = rb;
            fruit._collider = col;
            fruit.SetHeld(held);
            return fruit;
        }

        /// <summary>While held at the top the fruit is kinematic and non-colliding.</summary>
        public void SetHeld(bool held)
        {
            Dropped = !held;
            Body.bodyType = held ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            _collider.enabled = !held;
        }

        public void Drop()
        {
            SetHeld(false);
            DropTime = Time.time;
        }

        public float WorldRadius => SuikaCatalog.Radius(Level);

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (Merged || !Dropped)
                return;
            var other = collision.collider.GetComponent<SuikaFruit>();
            if (other == null || other.Merged || !other.Dropped || other.Level != Level)
                return;
            _game.RequestMerge(this, other);
        }
    }
}
