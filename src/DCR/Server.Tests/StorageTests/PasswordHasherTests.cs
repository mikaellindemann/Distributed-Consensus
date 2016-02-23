using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Server.Storage;

namespace Server.Tests.StorageTests
{
    [TestFixture]
    class PasswordHasherTests
    {
        [Test]
        public void GenerateSaltValueTests()
        {
            var saltValues = new List<string>();

            //Generate salts for every millisecond. Salts are generated from the current millisecond, 
            //so we have to make sure that some time passes between each generation.
            for (var i = 0; i < 10; i++)
            {
                saltValues.Add(PasswordHasher.GenerateSaltValue());
                Thread.Sleep(1);
            }

            //Check that they aren't the same.
            for (var i = 0; i < saltValues.Count-1; i++)
            {
                Assert.AreNotEqual(saltValues[i], saltValues[i + 1]);
            }
        }

        [Test]
        public void HashPasswordTest()
        {
            Assert.IsNull(PasswordHasher.HashPassword(null));
            Assert.DoesNotThrow(() => PasswordHasher.HashPassword(int.MaxValue.ToString() + int.MaxValue));

            Assert.AreEqual(136, PasswordHasher.HashPassword("testing").Length);
        }

        [Test]
        public void VerifyHashedPasswordTest()
        {
            var hashedPassword = PasswordHasher.HashPassword("testing");
            const string correctPassword = "testing";
            const string incorrectPassword = "testingTesting";

            Assert.IsFalse(PasswordHasher.VerifyHashedPassword(null, hashedPassword));
            Assert.IsFalse(PasswordHasher.VerifyHashedPassword(correctPassword, null));
            Assert.IsFalse(PasswordHasher.VerifyHashedPassword(incorrectPassword, hashedPassword));
            Assert.IsTrue(PasswordHasher.VerifyHashedPassword(correctPassword, hashedPassword));
        }
    }
}
