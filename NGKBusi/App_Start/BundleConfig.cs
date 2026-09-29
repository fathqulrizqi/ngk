using System;
using System.Web;
using System.Web.Optimization;

namespace NGKBusi
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js",
                        "~/Scripts/jquery-migrate-{version}.js",
                        "~/Scripts/jquery-ui-{version}.js"));
            
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new Bundle("~/bundles/bootstrap").Include(
                        "~/Scripts/respond.min.js",
                        "~/Scripts/select2.min.js",
                        "~/Scripts/autogrow.min.js",
                        "~/Scripts/umd/popper.js",
                        "~/Scripts/bootstrap.min.js",
                        "~/Scripts/inputmask/jquery.inputmask.bundle.min.js",
                        "~/Scripts/inputmask/inputmask/bindings/inputmask.binding.min.js",
                        "~/Scripts/clockpicker/bootstrap-clockpicker.min.js",
                        "~/Scripts/moment.min.js",
                        "~/Scripts/animsition/animsition.min.js",
                        "~/Scripts/tinymce/tinymce.min.js", 
                        "~/Content/sweetalert/sweetalert.min.js",
                        "~/Scripts/global.js",
                        "~/Scripts/loadingoverlay.min.js",
                        "~/Scripts/nav.js",
                        "~/Scripts/dialogs.js",
                        "~/Content/toastr/toastr.min.js"));

            bundles.Add(new StyleBundle("~/Content/styles").Include(
                      "~/Content/font-awesome/css/all.min.css",
                      "~/Content/bootstrap.min.css",
                      "~/Content/css/select2.min.css",
                      "~/Content/themes/base/jquery-ui.min.css",
                      "~/Content/clockpicker/bootstrap-clockpicker.min.css",
                      "~/Content/animsition/animsition.min.css",
                      "~/Content/site.css",
                      "~/Content/nav.css",
                      "~/Content/sweetalert/sweetalert.css",
                      "~/Content/toastr/toastr.min.css"));
        }
    }
}
