import React from 'react';

type ConfirmDialogProps = {
  title: string;
  body: string;
  onConfirm: () => void;
  onCancel: () => void;
  confirmLabel?: string;
  cancelLabel?: string;
  tone?: 'primary' | 'danger';
};

export default function ConfirmDialog({
  title,
  body,
  onConfirm,
  onCancel,
  confirmLabel = 'Подтвердить',
  cancelLabel = 'Отмена',
  tone = 'primary',
}: ConfirmDialogProps) {
  const confirmClass = tone === 'danger' ? 'danger' : 'primary';
  return (
    <div className="modal-backdrop">
      <div className="modal">
        <h3>{title}</h3>
        <p className="muted">{body}</p>
        <div className="flex">
          <button onClick={onCancel} className="ghost">{cancelLabel}</button>
          <button onClick={onConfirm} className={confirmClass}>{confirmLabel}</button>
        </div>
      </div>
    </div>
  );
}
