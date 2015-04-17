/*
 * Copyright 2015 Google Inc. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using UnityEngine;
using System.Collections;

public class EventManager : MonoBehaviour {
    private static EventManager _instance;

    public static EventManager instance {
        get {
            if(_instance == null) {
                _instance = GameObject.FindObjectOfType<EventManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }
            
            return _instance;
        }
    }

    public delegate void TangoServiceInitilizedHandler();
    public static event TangoServiceInitilizedHandler tangoServiceInitilized;

    public delegate void GameDataSavedHandler(bool successed);
    public static event GameDataSavedHandler gameDataSaved;

    public delegate void TangoPoseStateHandler(TangoPoseStates currentState);
    public static event TangoPoseStateHandler tangoPoseStatedChanged;

    void Awake () {
        if(_instance == null) {
            _instance = this;
            DontDestroyOnLoad(this);
        }
        else {
            if(this != _instance)
            {
                Destroy(this.gameObject);
            }
        }
    }

    public void TangoServiceInitializd () {
        if (tangoServiceInitilized != null) {
            tangoServiceInitilized();
        }
    }

    public void GameDataSaved(bool successed) {
        if (gameDataSaved != null) {
            gameDataSaved(successed);
        }
    }

    public void TangoPoseStateChanged(TangoPoseStates currentState) {
        if (tangoPoseStatedChanged != null) {
            tangoPoseStatedChanged(currentState);
        }
    }
}
