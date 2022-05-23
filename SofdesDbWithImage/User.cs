using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace SofdesDbWithImage;



public class User
{
    public User(int id, string name, DateTimeOffset birthday, byte[] picture)
    {
        Id = id;
        Name = name;
        Birthday = birthday;
        Picture = picture;
        var now = DateTimeOffset.Now;
        var age = now.Year - Birthday.Year;
        if (now.Month < Birthday.Month || (now.Month == Birthday.Month && now.Day < Birthday.Day))
        {
            age--;
        }
        Age = age;
    }

    public int Id { get; }
    public string Name { get; }
    public DateTimeOffset Birthday { get; }
    public byte[] Picture { get; }
    public int Age { get; }

    public UserEntity ToUserEntity()
    {
        return new UserEntity()
        {
            Id = Id,
            Name = Name,
            Birthday = Birthday,
            Picture = Picture
        };
    }
}

[Table("Users")]
public class UserEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset Birthday { get; set; }

    public byte[] Picture { get; set; }

    public User ToUser()
    {
        return new User(Id, Name, Birthday, Picture);
    }
}

public class UsersContext : DbContext
{
    public DbSet<UserEntity> UserEntities { get; set; }

    public string DbPath { get; }

    public UsersContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "users.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source={DbPath}");

}

public static class UsersDb
{
    public static User Get(int id)
    {
        var context = new UsersContext();
        var userEntity = context.UserEntities.Where(user => user.Id == id).FirstOrDefault();
        return userEntity?.ToUser();
    }
    public static List<User> GetAll()
    {
        var context = new UsersContext();
        var userEntities = context.UserEntities.ToList();
        return userEntities.Select(userEntity => userEntity.ToUser()).ToList();
    }

    public static void InsertUpdate(User user)
    {
        var context = new UsersContext();
        var userEntityOnDb = context.UserEntities.Where(userEntity => userEntity.Id == user.Id).FirstOrDefault();
        if (userEntityOnDb == null)
        {
            var userEntity = user.ToUserEntity();
            context.Add(userEntity);
            context.SaveChanges();
        }
        else
        {
            userEntityOnDb.Name = user.Name;
            userEntityOnDb.Birthday = user.Birthday;
            userEntityOnDb.Picture = user.Picture;
            context.SaveChanges();
        }
    }

    public static bool Delete(int id)
    {
        var context = new UsersContext();
        var userEntityOnDb = context.UserEntities.Where(userEntity => userEntity.Id == id).FirstOrDefault();
        if (userEntityOnDb == null)
        {
            return false;
        }
        context.Remove(userEntityOnDb);
        context.SaveChanges();
        return true;
    }
}