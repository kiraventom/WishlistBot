namespace WishlistBot.QueryParameters;

[Flags]
public enum WishPropertyType
{
   None = 0x0,
   Description = 0x1,
   Media = 0x2,
   Links = 0x4,
   Name = 0x8,
   Price = 0x16,
}
