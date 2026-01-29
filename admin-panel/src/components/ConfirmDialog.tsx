import React from 'react';

type ConfirmDialogProps = {
  title: string;
  body: string;
  onConfirm: () => void;
  onCancel: () => void;
};

export default function ConfirmDialog({ title, body, onConfirm, onCancel }: ConfirmDialogProps) {
  return (
    <div className="modal-backdrop">
      <div className="modal">
        <h3>{title}</h3>
        <p className="muted">{body}</p>
        <div className="flex">
          <button onClick={onCancel} className="ghost">Отмена</button>
          <button onClick={onConfirm} className="primary">Подтвердить</button>
        </div>
      </div>
    </div>
  );
}
