﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LCL.Reflection;
using LCL;
using System.Reflection;
using LCL.Utils;

namespace LCL
{
    /// <summary>
    /// 实体类型信息项
    /// </summary>
    public class EntityMatrix
    {
        #region 构造器 与 属性

        internal EntityMatrix(Type entity, Type list, Type repository)
        {
            this.EntityType = entity;
            this.ListType = list;
            this.RepositoryType = repository;
        }

        /// <summary>
        /// 实体类
        /// </summary>
        public Type EntityType { get; private set; }

        /// <summary>
        /// 实体类对应的列表类
        /// 
        /// Criteria是没有对应的列表类型的，所以这个属性可能为Null
        /// </summary>
        public Type ListType { get; private set; }

        /// <summary>
        /// 实体类对应的仓库类。
        /// 
        /// 如果找不到约定的仓库类，则这个属性为空。
        /// </summary>
        public Type RepositoryType { get; private set; }

        #endregion

        private const string RepositorySuffix = "Repository";

        public static void TryAddAssemblyRepository(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetExportedTypes())
                {
                    TryAddRepository(type);
                }
            }
        }
        public static void TryAddRepository(Type repositoryType)
        {
            var attri = repositoryType.GetSingleAttribute<RepositoryForAttribute>();
            if (attri != null)
            {
                Type entityType  = attri.EntityType;
                var listType = Convention_ListForEntity(entityType);
                var item = new EntityMatrix(entityType, listType, repositoryType);
                Add(item);
            }
        }

        /// <summary>
        /// 通过实体类找到约定项
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public static EntityMatrix FindByEntity(Type entityType)
        {
            if (entityType == null) throw new ArgumentNullException("entityType");

            var item = FastFindByEntityType(entityType);

            if (item == null)
            {
                var listType = Convention_ListForEntity(entityType);
                var rpType = Convention_RepositoryForEntity(entityType);
                item = new EntityMatrix(entityType, listType, rpType);
                Add(item);
            }

            return item;
        }

        /// <summary>
        /// 通过列表类找到约定项
        /// </summary>
        /// <param name="listType"></param>
        /// <returns></returns>
        public static EntityMatrix FindByList(Type listType)
        {
            if (listType == null) throw new ArgumentNullException("listType");

            var item = FastFindByListType(listType);

            if (item == null)
            {
                var entityType = Convention_EntityForList(listType);
                var rpType = Convention_RepositoryForEntity(entityType);
                item = new EntityMatrix(entityType, listType, rpType);
                Add(item);
            }

            return item;
        }

        /// <summary>
        /// 通过仓库类找到约定项
        /// </summary>
        /// <param name="repositoryType"></param>
        /// <returns></returns>
        public static EntityMatrix FindByRepository(Type repositoryType)
        {
            if (repositoryType == null) return null;

            var item = FastFindByRepositoryType(repositoryType);

            if (item == null)
            {
                var entityType = Convention_EntityForRepository(repositoryType);
                var listType = Convention_ListForEntity(entityType);
                item = new EntityMatrix(entityType, listType, repositoryType);
                Add(item);
            }

            return item;
        }

        /// <summary>
        /// 通过约定计算实体类对应的仓库类型的全名称。
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public static string RepositoryFullName(Type entityType)
        {
            return entityType.FullName + RepositorySuffix;
        }

        #region 实现快速的、线程安全的读取方法
        //以下内容使用三个高速的字典类存储约定项，以方便查询。

        private static ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private static IDictionary<Type, EntityMatrix> _entityIndex;
        private static IDictionary<Type, EntityMatrix> _listIndex;
        private static IDictionary<Type, EntityMatrix> _repositoryIndex;

        static EntityMatrix()
        {
            _entityIndex = new SortedDictionary<Type, EntityMatrix>(TypeNameComparer.Instance);
            _listIndex = new SortedDictionary<Type, EntityMatrix>(TypeNameComparer.Instance);
            _repositoryIndex = new SortedDictionary<Type, EntityMatrix>(TypeNameComparer.Instance);
        }

        private static EntityMatrix FastFindByEntityType(Type entityType)
        {
            try
            {
                _rwLock.EnterReadLock();

                EntityMatrix item = null;
                _entityIndex.TryGetValue(entityType, out item);
                return item;
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        private static EntityMatrix FastFindByListType(Type listType)
        {
            try
            {
                _rwLock.EnterReadLock();

                EntityMatrix item = null;
                _listIndex.TryGetValue(listType, out item);
                return item;
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        private static EntityMatrix FastFindByRepositoryType(Type repositoryType)
        {
            try
            {
                _rwLock.EnterReadLock();

                EntityMatrix item = null;
                _repositoryIndex.TryGetValue(repositoryType, out item);
                return item;
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        private static void Add(EntityMatrix item)
        {
            try
            {
                _rwLock.EnterReadLock();

                if (!_entityIndex.ContainsKey(item.EntityType))
                {
                    _entityIndex.Add(item.EntityType, item);
                    if (item.ListType != null)
                    {
                        _listIndex.Add(item.ListType, item);
                    }
                    if (item.RepositoryType != null)
                    {
                        _repositoryIndex.Add(item.RepositoryType, item);
                    }
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        #endregion

        #region Convetion
        //以下四个方法通过命名约定找到对应的类型

        internal static Type Convention_ListForEntity(Type entityType)
        {
            string listTypeName = entityType.FullName + "List";
            Type listType = entityType.Assembly.GetType(listTypeName);

            //目前有些类型并不满足约定。
            if (listType == null)
            {
                listTypeName = entityType.FullName + "s";
                listType = entityType.Assembly.GetType(listTypeName);
            }

            //Criteria是没有对应的列表类型的
            //if (listType == null) throw new ArgumentException("没有找到与entityType对应的列表类型");

            return listType;
        }

        private static Type Convention_EntityForList(Type listType)
        {
            Type entityType = null;

            if (listType.Name.EndsWith("List"))
            {
                var entityTypeName = listType.FullName.Substring(0, listType.FullName.Length - 4);
                entityType = listType.Assembly.GetType(entityTypeName);
            }

            if (entityType == null && listType.Name.EndsWith("s"))
            {
                var entityTypeName = listType.FullName.Substring(0, listType.FullName.Length - 1);
                entityType = listType.Assembly.GetType(entityTypeName);
            }

            if (entityType == null) throw new ArgumentException("没有找到与listType对应的实体类型");

            return entityType;
        }

        private static Type Convention_RepositoryForEntity(Type entityType)
        {
            string rpName = RepositoryFullName(entityType);
            Type rpType = entityType.Assembly.GetType(rpName);
            return rpType;
        }

        internal static Type Convention_EntityForRepository(Type repositoryType)
        {
            Type entityType = null;

            if (repositoryType.Name.EndsWith("Repository"))
            {
                var entityTypeName = repositoryType.FullName.Substring(0, repositoryType.FullName.Length - "Repository".Length);
                entityType = repositoryType.Assembly.GetType(entityTypeName);
            }

            if (entityType == null) throw new ArgumentException(string.Format("没有找到与 {0} 对应的实体类型", repositoryType));

            return entityType;
        }

        #endregion
    }
}
