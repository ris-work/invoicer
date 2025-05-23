using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;

namespace FormsGenerationGallery
{
    internal class MauiAppl : Application
    {
        public MauiAppl(Page page)
        {
            MainPage = page;
        }
    }
}
