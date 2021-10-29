﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
namespace Sean21.TDengineConnector
{
[ExecuteInEditMode]
public class TDChannel : MonoBehaviour
{
    public UnityEngine.Object target;
    public string databaseName;
    public string superTableName;
    public string tableName;
    public TDRequest request = new TDRequest();
    public bool usingSuperTable = true;
    public bool autoCreate = false;
    public bool insertSpecific = false;
    void OnEnable()
    {
        GetTarget();
        Initialize();
    }
    //Search for the first component that has either [DataTag] or [DataField] attribute, and make it target
    private void GetTarget()
    {
        if (target != null) return;
        Component[] components = GetComponents<Component>();
        foreach (Component comp in components) {
            UnityEngine.Object obj = comp as UnityEngine.Object;
            foreach (var field in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                DataTag dt = Attribute.GetCustomAttribute(field, typeof(DataTag)) as DataTag;
                DataField df = Attribute.GetCustomAttribute(field, typeof(DataField)) as DataField;
                if ( dt != null || df != null) {
                    target = obj;
                    components = null;
                    return;
                }
            }
        }
    }
    //Cache every Tag and Field in dictionaries to boost performance.
    private void Initialize()
    {
        if (string.IsNullOrEmpty(databaseName)) {
            databaseName = TDBridge.DefaultDatabaseName;
        }
        if (string.IsNullOrEmpty(superTableName)) {
            if (target != null) superTableName = target.GetType().Name;
        }
        if (string.IsNullOrEmpty(tableName)) {
            tableName = gameObject.name;
        }
        if (!request.channel) {
            request.channel = this;
        }
    }
    public void CreateDatabase()
    {
        request.sql = SQL.CreateDatabase(databaseName);
        StartCoroutine(request.Send());
    }

    public void CreateSuperTableForTarget()
    {
        request.sql = SQL.CreateSTable(target, databaseName, superTableName);
        StartCoroutine(request.Send());
    }
    public void DropSuperTableForTarget()
    {
        request.sql = "DROP STABLE IF EXISTS " + databaseName + "." + superTableName;
        StartCoroutine(request.Send());
    }
    public void CreateTableForTarget()
    {
        if (usingSuperTable) {
            request.sql = SQL.CreateTableUsing(target, databaseName, tableName, superTableName );
            StartCoroutine(request.Send());
        }
        else {
            request.sql = SQL.CreateTable(target, databaseName, tableName);
            StartCoroutine(request.Send());
        }
    }
    public void DropTableForTarget()
    {
        request.sql = "DROP TABLE IF EXISTS " + databaseName + "." + tableName;
        StartCoroutine(request.Send());
    }
    public void SendRequest()
    {
        StartCoroutine(request.Send());
    }
    public void SendRequest(string sql) {
        StartCoroutine(request.Send(sql));
    }
    public void Pull(params string[] column_names)
    {
        StartCoroutine(pull(column_names));
    }
    public IEnumerator pull(params string[] column_names)
    {        
        
        bool allTags = column_names.Length < 1?
                            true : false;
        
        string fieldNames = column_names.Length < 1?
                                "*":
                                string.Join(", ", column_names);
        yield return request.Send(SQL.GetLastRow(target, fieldNames, null, databaseName, tableName));
        if (!request.succeeded) {
            Debug.LogError("Failed Pulling fields! Please enable 'Detailed Debug Log' for detailes.");
            yield break;
        }
        TDBridge.FromTD(ref target, request.result);

    }
    public void Push(params string[] tag_names)
    {
        StartCoroutine(push(tag_names));
    }
    public IEnumerator push(params string[] tag_names) {
        List<string> sqls = SQL.SetTags(target, databaseName, tableName, tag_names);
        Debug.Log("sqls[0]: " + sqls[0]);
        foreach (string sql in sqls) {
            request.sql = sql;
            yield return request.Send();
        }
    }
    // public IEnumerator push() {
    //     List<string> sqls = SQL.SetTags(target, databaseName, tableName);
    //     foreach (string _sql in sqls) {
    //         request.sql = _sql;
    //         yield return request.Send();
    //     }
    // }
    public void Alter()
    {
        StartCoroutine(TDBridge.AlterSTableOf(target, databaseName, superTableName));
    }
    public void Insert()
    {
        if (autoCreate) {
            if (insertSpecific) {
                request.sql = SQL.InsertSpecificUsing(target, databaseName, superTableName, tableName );
            }
            else {
                request.sql = SQL.InsertUsing(target, databaseName, superTableName, tableName );
            }
        }
        else {
            if (insertSpecific) {
                request.sql = SQL.InsertSpecific(target, databaseName, tableName );
            }
            else {
                request.sql = SQL.Insert(target, databaseName, tableName );
            }
        }
        StartCoroutine(request.Send());
    }
    public void ClearRequest()
    {
        request.Clear();
    }
}
}
