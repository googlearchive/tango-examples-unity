//-----------------------------------------------------------------------
// <copyright file="RPCFileSender.cs" company="Google">
//
// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;
using Photon;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Utility class for sending big files over Photon RPC.
///
/// Photon can not reliably send multi-megabyte files over an RPC. This class splits up a file into multiple chunks
/// sent one at a time. To use this class call register for OnPackageTransferStarted, OnPackageReceived, and
/// OnPackageTransferFinished. Then call SendPackage with the raw bytes you want to send.
/// </summary>
/// <remarks>
/// This consist of code executed on two different devices: the sender and the receiver. All functions that start with
/// _Sender_ are called only on the sender. All functions that start with _Receiver_ are called only on the receiver.
/// OnPhotonPlayerDisconnected is called on both by PUN. SendPackage initiates the process from the sender.
/// _ClearState is called on both sender and receiver.
/// 
/// Execution order is as follows:
/// 1. SendPackage (called as normal public function.)
/// 2. _Receiver_StartTransfer (PUN RPC call on receiver.)
/// 3. _Sender_StartTransferACK (PUN RPC call on sender.)
/// 4. _Receiver_TransferBuffer (PUN RPC call on receiver.)
/// 5. _Sender_TransferBufferACK (PUN RPC call on sender.)
/// 6. 4 and 5 repeat until the full package is sent.
/// 7. _ClearState called on both sender and receiver.
/// </remarks>
[RequireComponent(typeof(PhotonView))]
public class RPCFileSender : Photon.PunBehaviour
{
    /// <summary>
    /// Max size for each package to send over network (in bytes).
    /// </summary>
    private const int MAX_SIZE_PER_PACKAGE = 50000;

    private byte[] m_receivedBuffer;

    private int m_senderPackageSendingIndex = 0;
    private int m_senderLastPackageSize = 0;
    private int m_totalPackages = 0;

    private PhotonPlayer m_receiver = null;
    private PhotonPlayer m_sender = null;
    private byte[] m_bytePackage;

    private bool m_isBusy = false;

    /// <summary>
    /// Delegate for OnPackageTransferStarted.
    /// </summary>
    /// <param name="packageSize">Total size of the buffer that is going to be transferred.</param>
    public delegate void OnPackageTransferStartedDelegate(int packageSize);
    
    /// <summary>
    /// Delegate for OnPackageReceived.
    /// </summary>
    /// <param name="finishedPercentage">The percentage of packages have been transferred.</param>
    public delegate void OnPackageReceivedDelegate(float finishedPercentage);
    
    /// <summary>
    /// Delegate for OnPackageTransferFinished.
    /// </summary>
    /// <param name="receivedByte">The full buffer that has been transferred.</param>
    public delegate void OnPackageTransferFinishedDelegate(byte[] receivedByte);

    /// <summary>
    /// Delegate for OnPackageTransferError.
    /// </summary>
    public delegate void OnPackageTransferErrorDelegate();

    /// <summary>
    /// Event raised on the receiver right before sending any data. This allows you to display a progress bar.
    /// </summary>
    public event OnPackageTransferStartedDelegate OnPackageTransferStarted;
    
    /// <summary>
    /// Event raised on the receiver as data is sent. This allows you to display a progress bar.
    /// </summary>
    public event OnPackageReceivedDelegate OnPackageReceived;
    
    /// <summary>
    /// Event raised on the receiver after all data has been sent. This provides the final data as a byte[].
    /// </summary>
    public event OnPackageTransferFinishedDelegate OnPackageTransferFinished;

    /// <summary>
    /// Event raised when encountered error.
    /// </summary>
    public event OnPackageTransferErrorDelegate OnPackageTransferError;

    /// <summary>
    /// Send a byte buffer to a photon player receiver.
    /// </summary>
    /// <param name="receiver">Receiver photon player.</param>
    /// <param name="byteBuffer">The buffer that is being sent over.</param>
    public void SendPackage(PhotonPlayer receiver, byte[] byteBuffer)
    {
        if (m_isBusy)
        {
            Debug.Log("Package sender is busy, try instantiate another instance to send.");
            OnPackageTransferError();
            return;
        }

        if (receiver == null)
        {
            Debug.LogError("Package receiver is null." + Environment.StackTrace);
            return;
        }

        m_isBusy = true;

        m_receiver = receiver;
        int size = byteBuffer.Length;
        m_totalPackages = size / MAX_SIZE_PER_PACKAGE;
        m_senderLastPackageSize = size % MAX_SIZE_PER_PACKAGE;

        m_bytePackage = byteBuffer;
        m_senderPackageSendingIndex = 0;

        GetComponent<PhotonView>().RPC("_Receiver_StartTransfer", m_receiver, size);
    }

    /// <summary>
    /// Called when a remote player left the room. This PhotonPlayer is already removed from the playerlist at this 
    /// time.
    /// </summary>
    /// <remarks>When your client calls PhotonNetwork.leaveRoom, PUN will call this method on the remaining clients.
    /// When a remote client drops connection or gets closed, this callback gets executed. after a timeout
    /// of several seconds.</remarks>
    /// <param name="otherPlayer">Other player.</param>
    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        if (otherPlayer == m_receiver || otherPlayer == m_sender)
        {
            _ClearState();
            OnPackageTransferError();
        }
    }

    /// <summary>
    /// On receiver, start the file transfer. Step 2 in the sending process.
    /// </summary>
    /// <param name="bufferSize">Total file size of the transfer (in bytes).</param>
    /// <param name="info">Sender message info.</param>
    [PunRPC]
    private void _Receiver_StartTransfer(int bufferSize, PhotonMessageInfo info)
    {
        if (m_isBusy)
        {
            Debug.Log("Package receiver is busy, try instantiate another instance to send.");
            GetComponent<PhotonView>().RPC("_Sender_StartTransferACK", info.sender, false);
            return;
        }

        m_isBusy = true;
        m_totalPackages = bufferSize / MAX_SIZE_PER_PACKAGE;
        m_receivedBuffer = new byte[bufferSize];
        OnPackageTransferStarted(bufferSize);
        m_sender = info.sender;
        GetComponent<PhotonView>().RPC("_Sender_StartTransferACK", info.sender, true);
    }

    /// <summary>
    /// On sender, acknowledge the transfer started. Step 3 in the sending process.
    /// </summary>
    /// <param name="isOkay"><c>True</c> if there's no error when _Receiver_StartTransfer is executed,
    /// otherwise <c>false</c></param>
    /// <param name="info">RPC mesage informaiton, used to get sender player.</param>
    [PunRPC]
    private void _Sender_StartTransferACK(bool isOkay, PhotonMessageInfo info)
    {
        if (!m_isBusy || info.sender != m_receiver)
        {
            Debug.LogError("_Sender_StartTransferACK called at a unexpected time" + Environment.StackTrace);
            return;
        }

        if (!isOkay)
        {
            _ClearState();
            OnPackageTransferError();
            return;
        }

        _Sender_TransferBuffer();
    }

    /// <summary>
    /// On receiver, RPC call for receiver to receive each single package. Step 4 in the sending process.
    /// </summary>
    /// <param name="receivedBuffer">Package of this transfer.</param>
    /// <param name="index">The index of this package.</param>
    /// <param name="info">RPC mesage informaiton, used to get sender player.</param>
    [PunRPC]
    private void _Receiver_TransferBuffer(byte[] receivedBuffer, int index, PhotonMessageInfo info)
    {
        if (!m_isBusy || info.sender != m_sender)
        {
            Debug.LogError("_Receiver_TransferBuffer at a unexpected time" + Environment.StackTrace);
            return;
        }
        
        if (index < m_totalPackages)
        {
            Array.Copy(receivedBuffer, 0, m_receivedBuffer, index * MAX_SIZE_PER_PACKAGE, MAX_SIZE_PER_PACKAGE);
            GetComponent<PhotonView>().RPC("_Sender_TransferBufferACK", info.sender);
            OnPackageReceived((float)index / (float)m_totalPackages);
        }
        else
        {
            Array.Copy(receivedBuffer, 0, 
                       m_receivedBuffer, m_receivedBuffer.Length - receivedBuffer.Length, receivedBuffer.Length);

            // _ClearState will set m_receivedBuffer to null, so we keep a reference of it.
            byte[] receivedBufferReference = m_receivedBuffer;
            _ClearState();
            OnPackageTransferFinished(receivedBufferReference);
        }
    }

    /// <summary>
    /// On sender, acknowledge the buffer is tranferred. Step 5 in the sending process.
    /// </summary>
    /// <param name="info">RPC mesage informaiton, used to get sender player.</param>
    [PunRPC]
    private void _Sender_TransferBufferACK(PhotonMessageInfo info)
    {
        if (!m_isBusy || info.sender != m_receiver)
        {
            Debug.LogError("_Sender_TransferBufferACK called at a unexpected time" + Environment.StackTrace);
            return;
        }

        _Sender_TransferBuffer();
    }

    /// <summary>
    /// On sender, send each package.
    /// </summary>
    private void _Sender_TransferBuffer()
    {
        if (m_senderPackageSendingIndex < m_totalPackages)
        {
            byte[] subarr = new byte[MAX_SIZE_PER_PACKAGE];
            Array.Copy(m_bytePackage, m_senderPackageSendingIndex * MAX_SIZE_PER_PACKAGE,
                       subarr, 0, MAX_SIZE_PER_PACKAGE);
            GetComponent<PhotonView>().RPC("_Receiver_TransferBuffer",
                                           m_receiver, subarr, m_senderPackageSendingIndex);
            m_senderPackageSendingIndex++;
        }
        else
        {
            // Last package.
            byte[] leftOverBytes = new byte[m_senderLastPackageSize];
            Array.Copy(m_bytePackage, m_totalPackages * MAX_SIZE_PER_PACKAGE, 
                       leftOverBytes, 0, m_senderLastPackageSize);
            GetComponent<PhotonView>().RPC("_Receiver_TransferBuffer",
                                           m_receiver, leftOverBytes, m_senderPackageSendingIndex);
            _ClearState();
        }
    }

    /// <summary>
    /// Clear all internal state for file transfer.
    /// </summary>
    private void _ClearState()
    {
        m_receivedBuffer = null;
        m_senderPackageSendingIndex = 0;
        m_senderLastPackageSize = 0;
        m_totalPackages = 0;
        m_receiver = null;
        m_sender = null;
        m_bytePackage = null;
        m_isBusy = false;
    }
}
