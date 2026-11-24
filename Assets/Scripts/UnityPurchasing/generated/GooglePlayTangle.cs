// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("aNpZemhVXlFy3hDer1VZWVldWFvbXQa5dX3FRvUuln9hkBdofjDlI4p64Y6RhktKc5ysZ9JUwf+kwyudKZ5xMho5uvipKGOCgdFESe/NFIraWVdYaNpZUlraWVlYl6uQ96fpdPrKE+pl8fx1eJm7u6t9qRRsmJXOFDkuKJbp97DpVG1bT8yKcviGiX1fn+bUICk9fLcSzUnRZ1l/tEzjXxfZSb6bfQSLv329NEnEhUFxihtqeg/eV69YMpylH8ITmUKNqeF66lmQhPr7Vz91E55jWkyH+Rxc9k6OjudY1KcRMgCBkv82vN8m2vGHnH35m3ycdT6/3niqjGcqfaJfLhuVSQ0Qa7s8YR0BFo/ArllL+EN9IheGssjTJWpI8QPXW1pbWVhZ");
        private static int[] order = new int[] { 0,13,6,12,13,8,13,11,9,12,12,11,13,13,14 };
        private static int key = 88;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
