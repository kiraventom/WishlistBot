namespace WishlistBot.QueryParameters;

[Flags]
public enum WishPropertyType
{
   None = 0b0,
   Description = 0b1,
   Media = 0b10,
   Links = 0b100,
   Name = 0b1000,
}
