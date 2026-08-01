"use client";

import { useEffect, useState } from "react";
import { useApp } from "@/lib/store";
import { farmerApi, authApi } from "@/lib/api";
import type { FarmerProfileDto } from "@/lib/types";
import { PageHeader } from "@/components/layout/DashboardShell";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Mail, Phone, User, Calendar, Save, Loader2, Shield, Leaf, Pencil } from "lucide-react";
import { formatDate, initials } from "@/lib/utils";
import { toast } from "sonner";

export function ProfilePage() {
  const user = useApp((s) => s.user);
  const setUser = useApp((s) => s.setUser);
  const [profile, setProfile] = useState<FarmerProfileDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [phone, setPhone] = useState("");
  const [saving, setSaving] = useState(false);

  // M22 FIX: Track whether user has made changes — disable Save until something is different
  const isDirty = profile !== null && (
    firstName !== (profile.firstName ?? "") ||
    lastName !== (profile.lastName ?? "") ||
    phone !== (profile.phoneNumber ?? "")
  );

  // Email change state
  const [emailDialogOpen, setEmailDialogOpen] = useState(false);
  const [emailStep, setEmailStep] = useState<"form" | "otp">("form");
  const [newEmail, setNewEmail] = useState("");
  const [currentPassword, setCurrentPassword] = useState("");
  const [emailOtp, setEmailOtp] = useState("");
  const [emailLoading, setEmailLoading] = useState(false);
  const [changeToken, setChangeToken] = useState("");

  useEffect(() => {
    farmerApi
      .me()
      .then((p) => {
        setProfile(p);
        setFirstName(p.firstName ?? "");
        setLastName(p.lastName ?? "");
        setPhone(p.phoneNumber ?? "");
      })
      .finally(() => setLoading(false));
  }, []);

  const onSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const updated = await farmerApi.updateProfile({
        firstName,
        lastName,
        phoneNumber: phone,
      });
      setProfile(updated);
      if (user) {
        setUser({ ...user, firstName, lastName, phoneNumber: phone });
      }
      toast.success("Profile updated");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Update failed");
    } finally {
      setSaving(false);
    }
  };

  const onEmailChangeRequest = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newEmail || !currentPassword) return;
    setEmailLoading(true);
    try {
      await authApi.changeEmail(newEmail, currentPassword);
      toast.success("Verification code sent to your new email");
      setEmailStep("otp");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Failed to initiate email change");
    } finally {
      setEmailLoading(false);
    }
  };

  const onEmailChangeConfirm = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!emailOtp || emailOtp.length !== 6) return;
    setEmailLoading(true);
    try {
      await authApi.confirmEmailChange(emailOtp, newEmail);
      toast.success("Email changed successfully");
      if (user) {
        setUser({ ...user, email: newEmail });
      }
      setProfile((p) => (p ? { ...p, email: newEmail } : p));
      setEmailDialogOpen(false);
      resetEmailDialog();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Verification failed");
    } finally {
      setEmailLoading(false);
    }
  };

  const resetEmailDialog = () => {
    setEmailStep("form");
    setNewEmail("");
    setCurrentPassword("");
    setEmailOtp("");
    setChangeToken("");
  };

  const onEmailDialogClose = (open: boolean) => {
    setEmailDialogOpen(open);
    if (!open) resetEmailDialog();
  };

  if (loading) return <Skeleton className="h-96 rounded-xl" />;

  return (
    <div>
      <PageHeader
        title="Your Profile"
        subtitle="Manage your personal information and account details."
      />

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Profile summary */}
        <Card className="lg:col-span-1">
          <CardContent className="p-6 text-center">
            <div className="relative inline-block">
              <Avatar className="h-24 w-24 bg-gradient-to-br from-emerald-500 to-green-700 text-white">
                <AvatarFallback className="bg-transparent text-white text-3xl font-serif">
                  {initials(profile?.firstName, profile?.lastName)}
                </AvatarFallback>
              </Avatar>
              <div className="absolute -bottom-1 -right-1 h-8 w-8 rounded-full bg-emerald-100 border-2 border-card grid place-items-center">
                <Shield className="h-4 w-4 text-emerald-700" />
              </div>
            </div>
            <h3 className="font-serif text-xl font-semibold mt-4">
              {profile?.firstName} {profile?.lastName}
            </h3>
            <p className="text-sm text-muted-foreground">{profile?.email}</p>
            <Badge className="mt-3 bg-emerald-100 text-emerald-700 border-0">
              <Leaf className="h-3 w-3 mr-1" />
              {profile?.role}
            </Badge>

            <div className="grid grid-cols-3 gap-2 mt-6 pt-6 border-t">
              <div>
                <div className="text-2xl font-bold font-serif">{profile?.totalFarms ?? 0}</div>
                <div className="text-xs text-muted-foreground uppercase tracking-wide">Farms</div>
              </div>
              <div>
                <div className="text-2xl font-bold font-serif">{profile?.totalPolicies ?? 0}</div>
                <div className="text-xs text-muted-foreground uppercase tracking-wide">Policies</div>
              </div>
              <div>
                <div className="text-2xl font-bold font-serif">{profile?.totalClaims ?? 0}</div>
                <div className="text-xs text-muted-foreground uppercase tracking-wide">Claims</div>
              </div>
            </div>

            <div className="mt-6 pt-6 border-t space-y-2 text-sm text-left">
              <div className="flex items-center gap-2 text-muted-foreground">
                <Calendar className="h-4 w-4" />
                Joined {formatDate(profile?.createdAt)}
              </div>
              {profile?.lastLoginAt && (
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Calendar className="h-4 w-4" />
                  Last login {formatDate(profile.lastLoginAt)}
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Edit form */}
        <Card className="lg:col-span-2">
          <CardContent className="p-6">
            <h3 className="font-serif text-lg font-semibold mb-5">Edit details</h3>
            <form onSubmit={onSave} className="space-y-4">
              <div className="grid sm:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="firstName">First name</Label>
                  <div className="relative">
                    <User className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      id="firstName"
                      value={firstName}
                      onChange={(e) => setFirstName(e.target.value)}
                      className="pl-9"
                      required
                    />
                  </div>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="lastName">Last name</Label>
                  <Input
                    id="lastName"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    required
                  />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">Email address</Label>
                <div className="flex items-center gap-2">
                  <div className="relative flex-1">
                    <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      id="email"
                      value={profile?.email ?? ""}
                      disabled
                      className="pl-9 bg-muted"
                    />
                  </div>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setEmailDialogOpen(true)}
                    className="shrink-0 gap-1.5"
                  >
                    <Pencil className="h-3.5 w-3.5" />
                    Change
                  </Button>
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="phone">Phone number</Label>
                <div className="relative">
                  <Phone className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    id="phone"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    className="pl-9"
                    placeholder="+91 98765 43210"
                  />
                </div>
              </div>
              <div className="flex justify-end pt-4 border-t">
                <Button
                  type="submit"
                  disabled={saving || !isDirty}
                  className="bg-emerald-700 hover:bg-emerald-800 text-white gap-1.5"
                >
                  {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                  Save changes
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>

      {/* Change Email Dialog */}
      <Dialog open={emailDialogOpen} onOpenChange={onEmailDialogClose}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {emailStep === "form" ? "Change email address" : "Verify new email"}
            </DialogTitle>
          </DialogHeader>

          {emailStep === "form" ? (
            <form onSubmit={onEmailChangeRequest} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="newEmail">New email address</Label>
                <Input
                  id="newEmail"
                  type="email"
                  value={newEmail}
                  onChange={(e) => setNewEmail(e.target.value)}
                  placeholder="you@example.com"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="currentPassword">Current password</Label>
                <Input
                  id="currentPassword"
                  type="password"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  placeholder="Enter your password"
                  required
                />
              </div>
              <DialogFooter>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => onEmailDialogClose(false)}
                >
                  Cancel
                </Button>
                <Button
                  type="submit"
                  disabled={emailLoading || !newEmail || !currentPassword}
                  className="bg-emerald-700 hover:bg-emerald-800 text-white"
                >
                  {emailLoading && <Loader2 className="h-4 w-4 animate-spin mr-1.5" />}
                  Send verification code
                </Button>
              </DialogFooter>
            </form>
          ) : (
            <form onSubmit={onEmailChangeConfirm} className="space-y-4">
              <p className="text-sm text-muted-foreground">
                A 6-digit code was sent to <span className="font-medium text-foreground">{newEmail}</span>.
                Enter it below to confirm.
              </p>
              <div className="space-y-2">
                <Label htmlFor="emailOtp">Verification code</Label>
                <Input
                  id="emailOtp"
                  value={emailOtp}
                  onChange={(e) => setEmailOtp(e.target.value.replace(/\D/g, "").slice(0, 6))}
                  placeholder="123456"
                  required
                  autoFocus
                  inputMode="numeric"
                  className="h-12 text-center text-2xl tracking-[0.5em] font-mono"
                />
              </div>
              <DialogFooter>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => {
                    setEmailStep("form");
                    setEmailOtp("");
                  }}
                >
                  Back
                </Button>
                <Button
                  type="submit"
                  disabled={emailLoading || emailOtp.length !== 6}
                  className="bg-emerald-700 hover:bg-emerald-800 text-white"
                >
                  {emailLoading && <Loader2 className="h-4 w-4 animate-spin mr-1.5" />}
                  Confirm change
                </Button>
              </DialogFooter>
            </form>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
