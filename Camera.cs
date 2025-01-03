﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.States;

// https://adambruenderman.wordpress.com/2011/04/05/create-a-2d-camera-in-xna-gs-4-0/
namespace Realm
{
    public class Camera
    {
        private const float zoomUpperLimit = 1.5f;
        private const float zoomLowerLimit = .5f;

        private float _zoom;
        private Matrix _transform;
        private Vector2 _pos;
        private float _rotation;
        private int _viewportWidth;
        private int _viewportHeight;
        private int _worldWidth;
        private int _worldHeight;

        public Camera(Viewport viewport, int worldWidth, int worldHeight, float initialZoom)
        {
            _zoom = initialZoom;
            _rotation = 0.0f;
            _pos = Player.Instance.Position;
            _viewportWidth = viewport.Width;
            _viewportHeight = viewport.Height;
            _worldWidth = worldWidth;
            _worldHeight = worldHeight;
        }

        public static void Reset()
        {
            Game1.Camera = new Camera(Game1.Viewport, Game1.WorldWidth, Game1.WorldHeight, 1f);
        }

        #region Properties

        public float Zoom
        {
            get { return _zoom; }
            set
            {
                _zoom = value;
                if (_zoom < zoomLowerLimit)
                    _zoom = zoomLowerLimit;
                if (_zoom > zoomUpperLimit)
                    _zoom = zoomUpperLimit;
            }
        }

        public float Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        public void Move(Vector2 amount)
        {
            _pos += amount;
        }

        public Vector2 Pos
        {
            get { return _pos; }
            set
            {
                float leftBarrier = (float)_viewportWidth * .5f / _zoom;
                float rightBarrier = _worldWidth - (float)_viewportWidth * .5f / _zoom;
                float topBarrier = _worldHeight - (float)_viewportHeight * .5f / _zoom;
                float bottomBarrier = (float)_viewportHeight * .5f / _zoom;
                _pos = value;
                if (_pos.X < leftBarrier)
                    _pos.X = leftBarrier;
                if (_pos.X > rightBarrier)
                    _pos.X = rightBarrier;
                if (_pos.Y > topBarrier)
                    _pos.Y = topBarrier;
                if (_pos.Y < bottomBarrier)
                    _pos.Y = bottomBarrier;
            }
        }

        #endregion

        public Matrix GetTransformation()
        {
            _transform =
                Matrix.CreateTranslation(new Vector3(-_pos.X, -_pos.Y, 0))
                * Matrix.CreateRotationZ(Rotation)
                * Matrix.CreateScale(new Vector3(Zoom, Zoom, 1))
                * Matrix.CreateTranslation(
                    new Vector3(_viewportWidth * 0.5f, _viewportHeight * 0.5f, 0)
                );

            return _transform;
        }
    }
}
