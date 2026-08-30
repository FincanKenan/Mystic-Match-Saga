namespace ZenMatch.UI
{
    public static class MainMenuOpenRequest
    {
        public static bool OpenShopOnLoad { get; set; }

        public static void RequestShop()
        {
            OpenShopOnLoad = true;
        }

        public static bool ConsumeShopRequest()
        {
            if (!OpenShopOnLoad)
                return false;

            OpenShopOnLoad = false;
            return true;
        }
    }
}