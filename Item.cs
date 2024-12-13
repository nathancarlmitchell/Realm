using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public abstract class Item : Entity
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public int MaximumStackableQuantity { get; set; }

        protected Item()
        {
            MaximumStackableQuantity = 1;
        }

        public Item(Texture2D image)
        {
            this.image = image;
            this.Radius = image.Width / 2;
        }

        public override void Update()
        {
            //throw new NotImplementedException();
        }
    }
}
