namespace MiniORM
{
    using System;
    using System.Linq;
    using MiniORM.Core;
    using MiniORM.Entities;
    using MiniORM.Interfaces;

    public class MainClass
    {
        public static void Main()
        {
            IDatatypeMatcher typeMatcher = new SqlDatatypeMatcher();
            var conStrBuilder = new ConnectionStringBuilder("MyTestingDatabase");
            var entityManager = new EntityManager(typeMatcher, conStrBuilder.ConnectionString, true);


            ////Test Persist
            //var testUser = new User("Peshkata", "123", 21, DateTime.Now);
            //entityManager.Persist(testUser);
            //entityManager.Persist(testUser);

            //testUser.Age = 32;

            //entityManager.Persist(testUser);

            //var testUser2 = new User("Goshkat1", "qwerty", 13, DateTime.Now.AddMonths(4));
            //entityManager.Persist(testUser2);
            //testUser2.Username = "Goshkata";
            //entityManager.Persist(testUser2);
            //entityManager.Persist(testUser2);

            //Test FindById
            //var dbUser = entityManager.FindById<User>(2);
            //Console.WriteLine(dbUser.Username + " " + dbUser.RegistrationDate);

            //Test FindAll + filter
            //entityManager.FindAll<User>("WHERE [Username] = 'Goshkata'").ToList().ForEach(u => Console.WriteLine(u.Username + " " + u.Password));

            //Test FindFirst + filter
            //var dbUser = entityManager.FindFirst<User>("WHERE [Username] = 'Goshkata'");
            //Console.WriteLine(dbUser.Username + " " + dbUser.RegistrationDate);


            //var dbUser = entityManager.FindFirst<User>("WHERE [Username] = 'Goshkata'");
            //Console.WriteLine(dbUser.Username + " " + dbUser.RegistrationDate);
            //dbUser.Age = 1234;
            //entityManager.Persist(dbUser);
            
        }
    }
}
