
namespace LagoVista.Core.UWP.IconFonts
{
    public static class IconFontSupport
    {
        public static void RegisterFonts()
        {
            LagoVista.XPlat.Core.Icons.Iconize.With(new EntypoPlusModule())
                .With(new FontAwesomeModule())
                .With(new IoniconsModule())
                .With(new MaterialModule())
                .With(new MeteoconsModule())
                .With(new SimpleLineIconsModule())
                .With(new TypiconsModule())
                .With(new WeatherIconsModule());
        }
    }
}