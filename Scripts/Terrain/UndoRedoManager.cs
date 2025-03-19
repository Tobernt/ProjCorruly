using System.Collections.Generic;
using UnityEngine;

public class UndoRedoManager
{
    private Stack<string> undoStack = new Stack<string>();
    private Stack<string> redoStack = new Stack<string>();

    public void SaveState(string state)
    {
        undoStack.Push(state);
        redoStack.Clear();
    }

    public string Undo()
    {
        if (undoStack.Count > 0)
        {
            string lastState = undoStack.Pop();
            redoStack.Push(lastState);
            return lastState;
        }
        return null;
    }

    public string Redo()
    {
        if (redoStack.Count > 0)
        {
            string nextState = redoStack.Pop();
            undoStack.Push(nextState);
            return nextState;
        }
        return null;
    }
}
