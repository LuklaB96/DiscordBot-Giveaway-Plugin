using Giveaway.Entities;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Giveaway.Helpers
{
    public class GiveawayUsers : IDisposable, IEnumerable<User>
    {
        private List<User> Users;
        private Random Random;
        public GiveawayUsers(IReadOnlyCollection<string> usersId)
        {
            InitalizeComponents();
            CreateUsersCollection(usersId);
        }
        private void InitalizeComponents()
        {
            Users = new List<User>();
            Random = new Random();
        }
        private void CreateUsersCollection(IReadOnlyCollection<string> usersId)
        {
            foreach (var userId in usersId)
            {
                User user = new User(ulong.Parse(userId));
                if (!Users.Contains(user))
                {
                    Users.Add(user);
                }
            }
        }
        public void AddUser(User user)
        {
            Users.Add(user);
        }
        public void RemoveUser(User user)
        {
            Users.Remove(user);
        }
        public void Dispose()
        {
            Users.Clear();
        }
        public IReadOnlyCollection<User> PickWinners(int amount)
        {
            List<User> winners = new List<User>();
            for(int i = 0; i < amount;i++)
            {
                int randomNumber = Random.Next(0, Users.Count);
                if (!winners.Contains(Users[randomNumber]))
                {
                    winners.Add(Users[randomNumber]);
                }
            }
            return winners;
        }
        public IReadOnlyCollection<User> GetAll()
        {
            return Users;
        }
        public IEnumerator<User> GetEnumerator()
        {
            for(int i = 0;i < Users.Count;i++)
            {
                yield return Users[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
