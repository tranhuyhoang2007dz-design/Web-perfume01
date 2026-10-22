using System.Web;
using System.Web.Optimization;

namespace Web_perfume
{
    public class BundleConfig
    {
        // Đăng ký các bundle (nhóm file CSS/JS) để tối ưu tải trang
        // Khi chạy Release mode, các file này sẽ được gộp và nén lại tự động
        public static void RegisterBundles(BundleCollection bundles)
        {
            // Bundle jQuery (thư viện JS cơ bản)
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            // Bundle validation (kiểm tra form)
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Bundle Bootstrap JS (cần có jQuery trước)
            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.bundle.js"));

            // Bundle CSS (Bootstrap + style riêng của website)
            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/Site.css",
                      "~/Content/luxury-style.css"));  // CSS phong cách luxury của website
        }
    }
}
